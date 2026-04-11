using Microsoft.EntityFrameworkCore;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Sightings;
using PawTrack.Infrastructure.Persistence;

namespace PawTrack.Infrastructure.Sightings;

public sealed class SightingRepository(PawTrackDbContext dbContext) : ISightingRepository
{
    public async Task<Sighting?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await dbContext.Sightings
            .AsTracking()
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Sighting>> GetByPetIdAsync(
        Guid petId, CancellationToken cancellationToken = default)
    {
        var results = await dbContext.Sightings
            .Where(s => s.PetId == petId)
            .OrderByDescending(s => s.SightedAt)
            .ToListAsync(cancellationToken);

        return results.AsReadOnly();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Sighting>> GetByLostEventIdAsync(
        Guid lostPetEventId, CancellationToken cancellationToken = default)
    {
        var results = await dbContext.Sightings
            .Where(s => s.LostPetEventId == lostPetEventId)
            .OrderByDescending(s => s.SightedAt)
            .ToListAsync(cancellationToken);

        return results.AsReadOnly();
    }

    public async Task<IReadOnlyList<Sighting>> GetInBBoxAsync(
        double north,
        double south,
        double east,
        double west,
        CancellationToken cancellationToken = default)
    {
        // No-tracking read — public map endpoint
        var results = await dbContext.Sightings
            .Where(s =>
                s.Lat >= south && s.Lat <= north &&
                s.Lng >= west  && s.Lng <= east)
            .OrderByDescending(s => s.ReportedAt)
            .ToListAsync(cancellationToken);

        return results.AsReadOnly();
    }

    public Task<bool> HasSightingsForLostEventSinceAsync(
        Guid lostPetEventId,
        DateTimeOffset sinceUtc,
        CancellationToken cancellationToken = default)
    {
        return dbContext.Sightings
            .AnyAsync(s =>
                    s.LostPetEventId == lostPetEventId
                    && s.SightedAt >= sinceUtc,
                cancellationToken);
    }

    public async Task AddAsync(Sighting sighting, CancellationToken cancellationToken = default) =>
        await dbContext.Sightings.AddAsync(sighting, cancellationToken);

    public void Update(Sighting sighting) =>
        dbContext.Sightings.Update(sighting);
}
