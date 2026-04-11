using Microsoft.EntityFrameworkCore;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Locations;
using PawTrack.Infrastructure.Persistence;

namespace PawTrack.Infrastructure.Locations;

public sealed class GeofencedAlertLogRepository(PawTrackDbContext dbContext)
    : IGeofencedAlertLogRepository
{
    public Task<bool> HasBeenAlertedAsync(
        Guid userId,
        Guid lostPetEventId,
        CancellationToken cancellationToken = default) =>
        dbContext.GeofencedAlertLogs
            .AnyAsync(g => g.UserId == userId && g.LostPetEventId == lostPetEventId,
                      cancellationToken);

    public async Task AddAsync(GeofencedAlertLog log, CancellationToken cancellationToken = default) =>
        await dbContext.GeofencedAlertLogs.AddAsync(log, cancellationToken);
}
