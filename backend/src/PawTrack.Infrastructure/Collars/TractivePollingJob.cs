using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PawTrack.Application.Collars.Interfaces;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Collars;
using PawTrack.Domain.Medical;
using PawTrack.Infrastructure.Medical;
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

        // ── Daily activity sync from track points ─────────────────────────────
        await SyncDailyActivityAsync(db, tractiveCollars, cancellationToken);
    }

    /// <summary>
    /// Computes daily walked distance from yesterday's CollarLocation track points using Haversine
    /// and creates an ActivityLog entry if one doesn't already exist.
    /// </summary>
    private async Task SyncDailyActivityAsync(
        PawTrackDbContext db,
        List<Collar> collars,
        CancellationToken ct)
    {
        var activityRepo = new ActivityLogRepository(db);
        var yesterday = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1));
        var windowStart = new DateTimeOffset(yesterday.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var windowEnd = new DateTimeOffset(yesterday.ToDateTime(TimeOnly.MaxValue), TimeSpan.Zero);

        foreach (var collar in collars)
        {
            if (await activityRepo.ExistsTractiveForDateAsync(collar.PetId, yesterday, ct)) continue;

            var points = await db.CollarLocations.AsNoTracking()
                .Where(c => c.CollarId == collar.Id
                    && c.RecordedAt >= windowStart
                    && c.RecordedAt <= windowEnd)
                .OrderBy(c => c.RecordedAt)
                .ToListAsync(ct);

            if (points.Count < 2) continue;

            // Haversine sum along track
            var totalMeters = 0.0;
            for (var i = 1; i < points.Count; i++)
            {
                totalMeters += PawTrack.Application.Common.GeoHelper.DistanceMetres(
                    points[i - 1].Lat, points[i - 1].Lng,
                    points[i].Lat, points[i].Lng);
            }

            var estimatedMinutes = Math.Max(1, (int)(totalMeters / 1000 * 12)); // ~12 min/km default pace
            var log = ActivityLog.Record(
                collar.PetId, collar.OwnerId, yesterday,
                ActivityType.Walk, estimatedMinutes,
                (int)totalMeters,
                "Distancia estimada desde collar GPS",
                ActivitySource.Tractive);

            await activityRepo.AddAsync(log, ct);
        }

        await db.SaveChangesAsync(ct);
    }
}
