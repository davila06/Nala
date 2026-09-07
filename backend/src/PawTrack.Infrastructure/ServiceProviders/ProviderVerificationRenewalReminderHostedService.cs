using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.ServiceProviders;

namespace PawTrack.Infrastructure.ServiceProviders;

public sealed class ProviderVerificationRenewalReminderHostedService(
    IServiceScopeFactory scopeFactory,
    IDistributedJobLock jobLock,
    ILogger<ProviderVerificationRenewalReminderHostedService> logger) : BackgroundService
{
    private static readonly TimeSpan CostaRicaOffset = TimeSpan.FromHours(-6);
    private static readonly TimeOnly ScheduledLocalTime = new(9, 0);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var localNow = DateTimeOffset.UtcNow.ToOffset(CostaRicaOffset);
            var nextRun = new DateTimeOffset(localNow.Year, localNow.Month, localNow.Day, ScheduledLocalTime.Hour, ScheduledLocalTime.Minute, 0, CostaRicaOffset);
            if (localNow >= nextRun) nextRun = nextRun.AddDays(1);
            try
            {
                await Task.Delay(nextRun - localNow, stoppingToken);
                await using var lease = await jobLock.TryAcquireAsync("ProviderVerificationRenewalReminder", TimeSpan.FromHours(1), stoppingToken);
                if (lease is null) continue;
                await using var scope = scopeFactory.CreateAsyncScope();
                await scope.ServiceProvider.GetRequiredService<ProviderVerificationRenewalReminderJob>().ExecuteAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception exception) { logger.LogError(exception, "Provider verification renewal reminder job failed."); }
        }
    }
}