using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PawTrack.Application.Common.Interfaces;

namespace PawTrack.Infrastructure.Notifications.Jobs;

/// <summary>
/// Runs <see cref="QrScanRetentionJob"/> once daily at 02:00 Costa Rica time (UTC-6),
/// using a low-traffic hour to minimise contention with user requests.
/// </summary>
public sealed class QrScanRetentionHostedService(
    IServiceScopeFactory scopeFactory,
    IDistributedJobLock jobLock,
    ILogger<QrScanRetentionHostedService> logger)
    : BackgroundService
{
    private static readonly TimeSpan CostaRicaOffset = TimeSpan.FromHours(-6);
    private static readonly TimeOnly ScheduledLocalTime = new(2, 0);

    private static readonly TimeSpan Interval = TimeSpan.FromHours(24);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var delay = GetDelayUntilNextRun(DateTimeOffset.UtcNow);
        logger.LogInformation("QrScanRetentionHostedService next run in {Delay}", delay);

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
        await using var lease = await jobLock.TryAcquireAsync("QrScanRetention", TimeSpan.FromHours(2), cancellationToken);
        if (lease is null) return;

        try
        {
            // QrScanRetentionJob depends on scoped services (EF DbContext)
            await using var scope = scopeFactory.CreateAsyncScope();
            var job = scope.ServiceProvider.GetRequiredService<QrScanRetentionJob>();
            await job.ExecuteAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // cancellation requested — exit cleanly
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "QrScanRetentionHostedService execution failed.");
        }
    }

    internal static TimeSpan GetDelayUntilNextRun(DateTimeOffset utcNow)
    {
        var localNow = utcNow.ToOffset(CostaRicaOffset);
        var localTodayAt2 = new DateTimeOffset(
            localNow.Year,
            localNow.Month,
            localNow.Day,
            ScheduledLocalTime.Hour,
            ScheduledLocalTime.Minute,
            0,
            CostaRicaOffset);

        var nextRun = localNow < localTodayAt2
            ? localTodayAt2
            : localTodayAt2.AddDays(1);

        return nextRun - localNow;
    }
}
