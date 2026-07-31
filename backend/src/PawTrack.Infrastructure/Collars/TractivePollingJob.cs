using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PawTrack.Application.Collars.Interfaces;
using PawTrack.Domain.Collars;
using PawTrack.Infrastructure.Persistence;

namespace PawTrack.Infrastructure.Collars;

/// <summary>Polls Tractive for active collars every 5 minutes and records their position.</summary>
public sealed class TractivePollingJob(
    IServiceProvider services,
    ILogger<TractivePollingJob> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(5));
        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            await PollAllActiveCollarsAsync(stoppingToken);
        }
    }

    private async Task PollAllActiveCollarsAsync(CancellationToken cancellationToken)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<PawTrackDbContext>();
        var tractive = scope.ServiceProvider.GetRequiredService<ITractiveService>();

        var tractiveCollars = await db.Collars
            .Where(c => c.IsActive
                && c.Provider == CollarProvider.Tractive
                && c.ExternalDeviceId != null
                && c.ExternalTokenEncrypted != null)
            .ToListAsync(cancellationToken);

        foreach (var collar in tractiveCollars)
        {
            try
            {
                var position = await tractive.GetLatestPositionAsync(
                    collar.ExternalTokenEncrypted!,
                    collar.ExternalDeviceId!,
                    cancellationToken);

                if (position is null) continue;

                collar.UpdateLocation(position.Lat, position.Lng, position.BatteryPercent);
                db.Collars.Update(collar);

                await db.CollarLocations.AddAsync(
                    CollarLocation.Record(collar.Id, position.Lat, position.Lng),
                    cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to poll collar {CollarId}", collar.Id);
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
