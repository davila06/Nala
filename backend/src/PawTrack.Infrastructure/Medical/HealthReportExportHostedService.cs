using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PawTrack.Application.Common.Interfaces;

namespace PawTrack.Infrastructure.Medical;

public sealed class HealthReportExportHostedService(
    IServiceScopeFactory scopeFactory,
    IDistributedJobLock jobLock,
    ILogger<HealthReportExportHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(15));
        do
        {
            await RunOnceAsync(stoppingToken);
        }
        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task RunOnceAsync(CancellationToken ct)
    {
        await using var lease = await jobLock.TryAcquireAsync("HealthReportExport", TimeSpan.FromHours(2), ct);
        if (lease is null) return;
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var job = scope.ServiceProvider.GetRequiredService<HealthReportExportJob>();
            await job.ExecuteAsync(ct);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "Health report export worker failed");
        }
    }
}
