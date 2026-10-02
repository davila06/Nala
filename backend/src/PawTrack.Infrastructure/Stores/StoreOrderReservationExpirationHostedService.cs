using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PawTrack.Application.Stores;

namespace PawTrack.Infrastructure.Stores;

public sealed class StoreOrderReservationExpirationHostedService(
    IServiceScopeFactory scopeFactory,
    ILogger<StoreOrderReservationExpirationHostedService> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var job = scope.ServiceProvider.GetRequiredService<StoreOrderReservationExpirationJob>();
                var count = await job.RunOnceAsync(DateTimeOffset.UtcNow, stoppingToken);
                if (count > 0)
                    logger.LogInformation("Expired {Count} store-order stock reservations.", count);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Store-order reservation expiration cycle failed.");
            }

            if (!await timer.WaitForNextTickAsync(stoppingToken)) break;
        }
    }
}