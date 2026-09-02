using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PawTrack.Application.Collars.Services;
using PawTrack.Application.Common.Interfaces;

namespace PawTrack.Infrastructure.Collars;

/// <summary>
/// Periodically checks active collars for connectivity loss (offline detection) and low
/// battery, dispatching at most one notification per cooldown window per condition.
/// Runs every 15 minutes; the first pass fires shortly after startup.
/// </summary>
public sealed class CollarConnectivityAlertJob(
    IServiceScopeFactory scopeFactory,
    IDistributedJobLock jobLock,
    ILogger<CollarConnectivityAlertJob> logger) : BackgroundService
{
    private static readonly TimeSpan InitialDelay = TimeSpan.FromSeconds(45);
    private static readonly TimeSpan RunInterval = TimeSpan.FromMinutes(15);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(InitialDelay, stoppingToken);

        using var timer = new PeriodicTimer(RunInterval);
        do
        {
            await RunCycleAsync(stoppingToken);
        }
        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task RunCycleAsync(CancellationToken cancellationToken)
    {
        await using var lease = await jobLock.TryAcquireAsync(
            "CollarConnectivityAlert", TimeSpan.FromMinutes(10), cancellationToken);
        if (lease is null)
            return; // another instance is already running this cycle

        await using var scope = scopeFactory.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<CollarConnectivityAlertService>();

        try
        {
            await service.RunOfflineDetectionAsync(cancellationToken);
            await service.RunBatteryAlertDetectionAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "CollarConnectivityAlertJob: cycle failed.");
        }
    }
}
