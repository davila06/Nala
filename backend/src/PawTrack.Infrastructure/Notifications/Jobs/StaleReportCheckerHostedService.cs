using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PawTrack.Application.Common.Interfaces;

namespace PawTrack.Infrastructure.Notifications.Jobs;

/// <summary>
/// Runs the stale report checker once per day at 08:00 Costa Rica time (UTC-6).
/// </summary>
public sealed class StaleReportCheckerHostedService(
    IServiceScopeFactory scopeFactory,
    IDistributedJobLock jobLock,
    ILogger<StaleReportCheckerHostedService> logger)
    : BackgroundService
{
    private static readonly TimeSpan CostaRicaOffset = TimeSpan.FromHours(-6);
    private static readonly TimeOnly ScheduledLocalTime = new(8, 0);

    private static readonly TimeSpan Interval = TimeSpan.FromHours(24);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var delay = GetDelayUntilNextRun(DateTimeOffset.UtcNow);
        logger.LogInformation("StaleReportCheckerHostedService next run in {Delay}", delay);

        await Task.Delay(delay, stoppingToken);

        using var timer = new PeriodicTimer(Interval);
        do
        {
            await RunCycleAsync(stoppingToken);
        }
        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task RunCycleAsync(CancellationToken cancellationToken)
    {
        await using var lease = await jobLock.TryAcquireAsync("StaleReportChecker", TimeSpan.FromHours(2), cancellationToken);
        if (lease is null) return;

        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var job = scope.ServiceProvider.GetRequiredService<StaleReportCheckerJob>();
            await job.ExecuteAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // cancellation requested — exit cleanly
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "StaleReportCheckerHostedService execution failed.");
        }
    }

    internal static TimeSpan GetDelayUntilNextRun(DateTimeOffset utcNow)
    {
        var localNow = utcNow.ToOffset(CostaRicaOffset);
        var localTodayAt8 = new DateTimeOffset(
            localNow.Year,
            localNow.Month,
            localNow.Day,
            ScheduledLocalTime.Hour,
            ScheduledLocalTime.Minute,
            0,
            CostaRicaOffset);

        var nextRunLocal = localNow < localTodayAt8
            ? localTodayAt8
            : localTodayAt8.AddDays(1);

        var delay = nextRunLocal - localNow;
        return delay <= TimeSpan.Zero ? TimeSpan.FromMinutes(1) : delay;
    }
}
