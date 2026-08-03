using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PawTrack.Application.Common.Interfaces;

namespace PawTrack.Infrastructure.Clinics;

/// <summary>
/// Runs once daily at 03:00 Costa Rica time (UTC-6) and deletes
/// ClinicProfileView rows older than 90 days to control table growth.
/// </summary>
public sealed class ClinicProfileViewPurgeHostedService(
    IServiceScopeFactory scopeFactory,
    ILogger<ClinicProfileViewPurgeHostedService> logger)
    : BackgroundService
{
    private const int RetentionDays = 90;
    private static readonly TimeSpan CrOffset = TimeSpan.FromHours(-6);
    private static readonly TimeOnly ScheduledLocalTime = new(3, 0);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = GetDelayUntilNextRun(DateTimeOffset.UtcNow);
            logger.LogInformation("ClinicProfileViewPurgeHostedService next run in {Delay}", delay);

            await Task.Delay(delay, stoppingToken);
            if (stoppingToken.IsCancellationRequested) break;

            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var repo = scope.ServiceProvider.GetRequiredService<IClinicProfileViewRepository>();
                var uow  = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                await repo.PruneOlderThanAsync(RetentionDays, stoppingToken);
                await uow.SaveChangesAsync(stoppingToken);
                logger.LogInformation("ClinicProfileViewPurgeHostedService: pruned rows older than {Days} days.", RetentionDays);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "ClinicProfileViewPurgeHostedService execution failed.");
            }
        }
    }

    internal static TimeSpan GetDelayUntilNextRun(DateTimeOffset utcNow)
    {
        var localNow = utcNow.ToOffset(CrOffset);
        var nextRun = localNow.Date == localNow.Date && localNow.TimeOfDay < ScheduledLocalTime.ToTimeSpan()
            ? localNow.Date.Add(ScheduledLocalTime.ToTimeSpan())
            : localNow.Date.AddDays(1).Add(ScheduledLocalTime.ToTimeSpan());
        return nextRun - localNow.DateTime;
    }
}
