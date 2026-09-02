using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Subscriptions.Interfaces;
using PawTrack.Domain.Subscriptions;

namespace PawTrack.Infrastructure.Subscriptions;

/// <summary>
/// Sends renewal reminder emails 7 days before expiry and expiration notices on the day
/// a subscription lapses. Runs once daily at 09:00 Costa Rica time (UTC-6).
/// </summary>
public sealed class SubscriptionRenewalNotificationJob(
    IServiceScopeFactory scopeFactory,
    ILogger<SubscriptionRenewalNotificationJob> logger) : BackgroundService
{
    private static readonly TimeSpan CostaRicaOffset = TimeSpan.FromHours(-6);
    private static readonly TimeOnly ScheduledLocalTime = new(9, 0);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = GetDelayUntilNextRun(DateTimeOffset.UtcNow);
            await Task.Delay(delay, stoppingToken);
            if (stoppingToken.IsCancellationRequested) break;

            await RunAsync(stoppingToken);
        }
    }

    private async Task RunAsync(CancellationToken ct)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var subscriptionRepo = scope.ServiceProvider.GetRequiredService<ISubscriptionRepository>();
            var userRepo = scope.ServiceProvider.GetRequiredService<IUserRepository>();
            var emailSender = scope.ServiceProvider.GetRequiredService<IEmailSender>();

            // 7-day reminder
            var expiringSoon = await subscriptionRepo.GetExpiringWithinAsync(7, ct);
            foreach (var sub in expiringSoon)
            {
                if (sub.UserId is null || sub.ExpiresAt is null) continue;
                var user = await userRepo.GetByIdAsync(sub.UserId.Value, ct);
                if (user is null) continue;
                try
                {
                    await emailSender.SendSubscriptionExpiringAsync(
                        user.Email, user.Name, TierLabel(sub.Tier), sub.ExpiresAt.Value, ct);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    logger.LogWarning(ex, "Failed to send expiry reminder to {UserId}", sub.UserId);
                }
            }

            // Expiration notice for subscriptions that just lapsed (within the last 24h)
            var justExpired = await subscriptionRepo.GetExpiredActiveAsync(ct);
            foreach (var sub in justExpired)
            {
                if (sub.UserId is null) continue;
                var user = await userRepo.GetByIdAsync(sub.UserId.Value, ct);
                if (user is null) continue;
                try
                {
                    await emailSender.SendSubscriptionExpiredAsync(
                        user.Email, user.Name, TierLabel(sub.Tier), ct);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    logger.LogWarning(ex, "Failed to send expiry notice to {UserId}", sub.UserId);
                }
            }

            logger.LogInformation(
                "[RenewalNotification] Reminders={Reminders} ExpiredNotices={Expired}",
                expiringSoon.Count, justExpired.Count);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "[RenewalNotification] Job failed.");
        }
    }

    private static string TierLabel(SubscriptionTier tier) => tier switch
    {
        SubscriptionTier.UserPlus => "Plus",
        SubscriptionTier.UserFamilia => "Familia",
        SubscriptionTier.ClinicPlus => "Clínica Plus",
        SubscriptionTier.ClinicPartner => "Clínica Partner",
        SubscriptionTier.StorePlus => "Tienda Plus",
        SubscriptionTier.StorePartner => "Tienda Partner",
        SubscriptionTier.ShelterPlus => "Refugio Plus",
        SubscriptionTier.MuniBasica => "Municipalidad Básica",
        SubscriptionTier.MuniFull => "Municipalidad Full",
        SubscriptionTier.MuniRedRegional => "Red Regional",
        _ => tier.ToString(),
    };

    private static TimeSpan GetDelayUntilNextRun(DateTimeOffset utcNow)
    {
        var local = utcNow.ToOffset(CostaRicaOffset);
        var nextDate = local.Date;
        if (local.TimeOfDay >= ScheduledLocalTime.ToTimeSpan())
            nextDate = nextDate.AddDays(1);
        var nextRun = new DateTimeOffset(
            nextDate.Year, nextDate.Month, nextDate.Day,
            ScheduledLocalTime.Hour, ScheduledLocalTime.Minute, 0,
            CostaRicaOffset);
        return nextRun - utcNow;
    }
}
