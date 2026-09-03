using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PawTrack.Application.Common.Interfaces;

namespace PawTrack.Infrastructure.Compliance;

/// <summary>
/// Runs <see cref="PersonalDataRetentionJob"/> once daily at 03:00 Costa Rica time (UTC-6),
/// a low-traffic hour, mirroring <c>QrScanRetentionHostedService</c>'s scheduling pattern.
/// </summary>
public sealed class PersonalDataRetentionHostedService(
    IServiceScopeFactory scopeFactory,
    IDistributedJobLock jobLock,
    ILogger<PersonalDataRetentionHostedService> logger)
    : BackgroundService
{
    private static readonly TimeSpan CostaRicaOffset = TimeSpan.FromHours(-6);
    private static readonly TimeOnly ScheduledLocalTime = new(3, 0);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = GetDelayUntilNextRun(DateTimeOffset.UtcNow);
            logger.LogInformation("PersonalDataRetentionHostedService next run in {Delay}", delay);

            await Task.Delay(delay, stoppingToken);
            if (stoppingToken.IsCancellationRequested)
                break;

            await using var lease = await jobLock.TryAcquireAsync("PersonalDataRetention", TimeSpan.FromHours(2), stoppingToken);
            if (lease is null) continue;

            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var job = scope.ServiceProvider.GetRequiredService<PersonalDataRetentionJob>();
                await job.ExecuteAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "PersonalDataRetentionHostedService execution failed.");
            }
        }
    }

    internal static TimeSpan GetDelayUntilNextRun(DateTimeOffset utcNow)
    {
        var localNow = utcNow.ToOffset(CostaRicaOffset);
        var localTodayAt3 = new DateTimeOffset(
            localNow.Year,
            localNow.Month,
            localNow.Day,
            ScheduledLocalTime.Hour,
            ScheduledLocalTime.Minute,
            0,
            CostaRicaOffset);

        var nextRun = localNow < localTodayAt3
            ? localTodayAt3
            : localTodayAt3.AddDays(1);

        return nextRun - localNow;
    }
}
