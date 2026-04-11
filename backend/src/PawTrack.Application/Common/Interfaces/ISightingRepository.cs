using PawTrack.Domain.Sightings;

namespace PawTrack.Application.Common.Interfaces;

public interface ISightingRepository
{
    Task<Sighting?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Returns all sightings for a pet, ordered by SightedAt descending.</summary>
    Task<IReadOnlyList<Sighting>> GetByPetIdAsync(
        Guid petId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns all sightings linked to the given lost-pet event, ordered by
    /// <see cref="Sighting.SightedAt"/> descending.
    /// Used by the Case Room aggregation query.
    /// </summary>
    Task<IReadOnlyList<Sighting>> GetByLostEventIdAsync(
        Guid lostPetEventId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns sightings within a bounding box (lat/lng range), ordered by ReportedAt descending.
    /// Used for the public map endpoint.
    /// </summary>
    Task<IReadOnlyList<Sighting>> GetInBBoxAsync(
        double north,
        double south,
        double east,
        double west,
        CancellationToken cancellationToken = default);

    Task<bool> HasSightingsForLostEventSinceAsync(
        Guid lostPetEventId,
        DateTimeOffset sinceUtc,
        CancellationToken cancellationToken = default);

    Task AddAsync(Sighting sighting, CancellationToken cancellationToken = default);
    void Update(Sighting sighting);
}
