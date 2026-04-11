using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace PawTrack.Infrastructure.Notifications.Jobs;

/// <summary>
/// Runs <see cref="QrScanRetentionJob"/> once daily at 02:00 Costa Rica time (UTC-6),
/// using a low-traffic hour to minimise contention with user requests.
/// </summary>
public sealed class QrScanRetentionHostedService(
    IServiceScopeFactory scopeFactory,
    ILogger<QrScanRetentionHostedService> logger)
    : BackgroundService
{
    private static readonly TimeSpan CostaRicaOffset = TimeSpan.FromHours(-6);
    private static readonly TimeOnly ScheduledLocalTime = new(2, 0);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = GetDelayUntilNextRun(DateTimeOffset.UtcNow);
            logger.LogInformation("QrScanRetentionHostedService next run in {Delay}", delay);

            await Task.Delay(delay, stoppingToken);
            if (stoppingToken.IsCancellationRequested)
                break;

            try
            {
                // QrScanRetentionJob depends on scoped services (EF DbContext)
                await using var scope = scopeFactory.CreateAsyncScope();
                var job = scope.ServiceProvider.GetRequiredService<QrScanRetentionJob>();
                await job.ExecuteAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "QrScanRetentionHostedService execution failed.");
            }
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
