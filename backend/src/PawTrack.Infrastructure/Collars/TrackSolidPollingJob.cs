using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PawTrack.Application.Collars.Interfaces;
using PawTrack.Application.Collars.Services;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Collars;
using PawTrack.Infrastructure.Persistence;

namespace PawTrack.Infrastructure.Collars;

/// <summary>
/// Background service that polls the TrackSolid Pro Open API for active Jimi IoT AL600 collars.
/// Runs a high-frequency 30s tick for collars currently in lost mode (emergency search tracking),
/// and a 5-minute interval for normal active collars to optimize API quotas and battery.
/// Uses a distributed lock to ensure single execution across horizontally scaled replicas.
/// </summary>
public sealed class TrackSolidPollingJob(
    IServiceScopeFactory scopeFactory,
    IDistributedJobLock jobLock,
    ILogger<TrackSolidPollingJob> logger) : BackgroundService
{
    private static readonly TimeSpan LostModeInterval = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan NormalInterval = TimeSpan.FromMinutes(5);
    private DateTimeOffset _lastNormalPoll = DateTimeOffset.MinValue;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(LostModeInterval);
        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            var now = DateTimeOffset.UtcNow;
            var includeNormalCollars = now - _lastNormalPoll >= NormalInterval;
            if (includeNormalCollars)
                _lastNormalPoll = now;

            await PollActiveCollarsAsync(stoppingToken, includeNormalCollars);
        }
    }

    private async Task PollActiveCollarsAsync(CancellationToken cancellationToken, bool includeNormalCollars)
    {
        await using var lease = await jobLock.TryAcquireAsync(
            "TrackSolidPolling", TimeSpan.FromMinutes(2), cancellationToken);
        if (lease is null)
            return; // Another instance is currently executing the poll cycle

        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<PawTrackDbContext>();
        var trackSolid = scope.ServiceProvider.GetRequiredService<ITrackSolidService>();
        var safeZoneEvaluationService = scope.ServiceProvider.GetRequiredService<CollarSafeZoneEvaluationService>();

        var query = db.Collars
            .Where(c => c.IsActive
                && c.Provider == CollarProvider.JimiTrackSolid
                && c.ExternalDeviceId != null);

        if (!includeNormalCollars)
            query = query.Where(c => c.IsLost);

        var collars = await query.ToListAsync(cancellationToken);
        if (collars.Count == 0)
            return;

        var imeis = collars
            .Select(c => c.ExternalDeviceId!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var positions = await trackSolid.GetBatchPositionsAsync(imeis, cancellationToken);
        if (positions.Count == 0)
            return;

        var modifiedCount = 0;
        foreach (var collar in collars)
        {
            if (!positions.TryGetValue(collar.ExternalDeviceId!, out var pos))
                continue;

            collar.UpdateLocation(pos.Lat, pos.Lng, pos.BatteryPercent);
            db.Collars.Update(collar);

            await db.CollarLocations.AddAsync(
                CollarLocation.Record(collar.Id, pos.Lat, pos.Lng, pos.AccuracyMeters),
                cancellationToken);

            if (collar.IsLost && collar.LostPetEventId is not null)
            {
                var lostPetEvent = await db.LostPetEvents
                    .FirstOrDefaultAsync(e => e.Id == collar.LostPetEventId.Value, cancellationToken);
                if (lostPetEvent is not null)
                {
                    lostPetEvent.UpdateLastSeenLocation(pos.Lat, pos.Lng, DateTimeOffset.UtcNow);
                    db.LostPetEvents.Update(lostPetEvent);
                }
            }

            await safeZoneEvaluationService.EvaluateAsync(collar, pos.Lat, pos.Lng, cancellationToken);
            modifiedCount++;
        }

        if (modifiedCount > 0)
        {
            await db.SaveChangesAsync(cancellationToken);
            logger.LogDebug("TrackSolidPollingJob: successfully updated {Count} collar locations.", modifiedCount);
        }
    }
}
