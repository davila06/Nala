using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace PawTrack.Infrastructure.Notifications.Jobs;

/// <summary>
/// Runs the stale report checker once per day at 08:00 Costa Rica time (UTC-6).
/// </summary>
public sealed class StaleReportCheckerHostedService(
    IServiceScopeFactory scopeFactory,
    ILogger<StaleReportCheckerHostedService> logger)
    : BackgroundService
{
    private static readonly TimeSpan CostaRicaOffset = TimeSpan.FromHours(-6);
    private static readonly TimeOnly ScheduledLocalTime = new(8, 0);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = GetDelayUntilNextRun(DateTimeOffset.UtcNow);
            logger.LogInformation("StaleReportCheckerHostedService next run in {Delay}", delay);

            await Task.Delay(delay, stoppingToken);
            if (stoppingToken.IsCancellationRequested)
                break;

            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var job = scope.ServiceProvider.GetRequiredService<StaleReportCheckerJob>();
                await job.ExecuteAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "StaleReportCheckerHostedService execution failed.");
            }
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
