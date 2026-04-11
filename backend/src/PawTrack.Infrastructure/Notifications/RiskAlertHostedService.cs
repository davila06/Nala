using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Notifications;

namespace PawTrack.Infrastructure.Notifications;

/// <summary>
/// Background service that fires preventive risk alerts (Mejora D).
/// <para>
/// Runs once per day at 13:00 UTC (07:00 Costa Rica, UTC-6).
/// For each <see cref="RiskCalendarEvent"/> whose <c>AlertTriggerDate</c> matches today,
/// it creates a <see cref="Notification"/> record and sends a push notification to every
/// owner who has not opted out of preventive alerts.
/// </para>
/// </summary>
public sealed class RiskAlertHostedService(
    IServiceScopeFactory scopeFactory,
    ILogger<RiskAlertHostedService> logger)
    : BackgroundService
{
    // 13:00 UTC = 07:00 Costa Rica (UTC-6, no DST)
    private static readonly TimeSpan DailyRunTime = TimeSpan.FromHours(13);

    /// <summary>Initial warm-up delay so the app finishes starting before the first run.</summary>
    private static readonly TimeSpan InitialDelay = TimeSpan.FromSeconds(60);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation(
            "RiskAlertHostedService started. First run in {Delay}.", InitialDelay);

        await Task.Delay(InitialDelay, stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = ComputeDelayUntilNextRun();

            logger.LogDebug(
                "RiskAlertHostedService: next run in {Delay:hh\\:mm\\:ss}.", delay);

            await Task.Delay(delay, stoppingToken);

            if (!stoppingToken.IsCancellationRequested)
            {
                await RunDailyCycleAsync(stoppingToken);
            }
        }
    }

    private async Task RunDailyCycleAsync(CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();

        var calendarRepo     = scope.ServiceProvider.GetRequiredService<IRiskCalendarEventRepository>();
        var preferencesRepo  = scope.ServiceProvider.GetRequiredService<IUserNotificationPreferencesRepository>();
        var notificationRepo = scope.ServiceProvider.GetRequiredService<INotificationRepository>();
        var pushService      = scope.ServiceProvider.GetRequiredService<IPushNotificationService>();
        var unitOfWork       = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        IReadOnlyList<RiskCalendarEvent> events;
        try
        {
            events = await calendarRepo.GetByTriggerDateAsync(today, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "RiskAlertHostedService: failed to load calendar events for {Date}.", today);
            return;
        }

        if (events.Count == 0)
        {
            logger.LogDebug("RiskAlertHostedService: no risk events triggered for {Date}.", today);
            return;
        }

        IReadOnlyList<Guid> userIds;
        try
        {
            userIds = await preferencesRepo.GetUserIdsWithPreventiveAlertsEnabledAsync(ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "RiskAlertHostedService: failed to load eligible user IDs.");
            return;
        }

        if (userIds.Count == 0)
        {
            logger.LogInformation(
                "RiskAlertHostedService: {EventCount} event(s) triggered but no eligible users.", events.Count);
            return;
        }

        logger.LogInformation(
            "RiskAlertHostedService: sending {EventCount} event(s) to {UserCount} user(s) for {Date}.",
            events.Count, userIds.Count, today);

        foreach (var evt in events)
        {
            var title = evt.Name;
            var body  = evt.MessageTemplate;

            foreach (var userId in userIds)
            {
                var notification = Notification.Create(
                    userId,
                    NotificationType.PreventiveAlert,
                    title,
                    body,
                    evt.Id.ToString());

                await notificationRepo.AddAsync(notification, ct);

                try
                {
                    await pushService.SendAsync(userId, title, body, cancellationToken: ct);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    // Push delivery failure is non-fatal — the in-app notification is already saved.
                    logger.LogWarning(
                        ex,
                        "RiskAlertHostedService: push notification failed for user {UserId}, event '{EventName}'.",
                        userId, evt.Name);
                }
            }
        }

        try
        {
            await unitOfWork.SaveChangesAsync(ct);

            logger.LogInformation(
                "RiskAlertHostedService: persisted notifications for {Date}.", today);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "RiskAlertHostedService: failed to persist notifications for {Date}.", today);
        }
    }

    /// <summary>
    /// Returns the <see cref="TimeSpan"/> until the next 13:00 UTC.
    /// If the current UTC time is already past 13:00, schedules for 13:00 tomorrow.
    /// </summary>
    private static TimeSpan ComputeDelayUntilNextRun()
    {
        var now      = DateTime.UtcNow;
        var nextRun  = now.Date.Add(DailyRunTime);

        if (now >= nextRun)
        {
            nextRun = nextRun.AddDays(1);
        }

        return nextRun - now;
    }
}

