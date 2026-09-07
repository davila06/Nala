using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PawTrack.Application.Common.Interfaces;

namespace PawTrack.Infrastructure.Certificates;

public sealed class VerificationRenewalReminderHostedService(
    IServiceScopeFactory scopeFactory,
    IDistributedJobLock jobLock,
    ILogger<VerificationRenewalReminderHostedService> logger)
    : BackgroundService
{
    private static readonly TimeSpan CostaRicaOffset = TimeSpan.FromHours(-6);
    private static readonly TimeOnly ScheduledLocalTime = new(9, 0);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = GetDelayUntilNextRun(DateTimeOffset.UtcNow);
            logger.LogInformation("VerificationRenewalReminderHostedService next run in {Delay}", delay);
            await Task.Delay(delay, stoppingToken);
            if (stoppingToken.IsCancellationRequested) break;

            await using var lease = await jobLock.TryAcquireAsync("VerificationRenewalReminder", TimeSpan.FromHours(1), stoppingToken);
            if (lease is null) continue;

            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var job = scope.ServiceProvider.GetRequiredService<VerificationRenewalReminderJob>();
                await job.ExecuteAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "VerificationRenewalReminderHostedService execution failed.");
            }
        }
    }

    internal static TimeSpan GetDelayUntilNextRun(DateTimeOffset utcNow)
    {
        var localNow = utcNow.ToOffset(CostaRicaOffset);
        var localRun = new DateTimeOffset(localNow.Year, localNow.Month, localNow.Day,
            ScheduledLocalTime.Hour, ScheduledLocalTime.Minute, 0, CostaRicaOffset);
        var nextRun = localNow < localRun ? localRun : localRun.AddDays(1);
        return nextRun - localNow;
    }
}
