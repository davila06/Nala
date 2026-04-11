using PawTrack.Domain.LostPets;

namespace PawTrack.Application.Common.Interfaces;

/// <summary>
/// Persistence contract for <see cref="SearchZone"/> aggregates.
/// </summary>
public interface ISearchZoneRepository
{
    /// <summary>Returns all zones for a given lost-pet event, ordered by label.</summary>
    Task<IReadOnlyList<SearchZone>> GetByLostPetEventIdAsync(Guid lostPetEventId, CancellationToken cancellationToken);

    /// <summary>Returns a single zone, or <c>null</c> if not found.</summary>
    Task<SearchZone?> GetByIdAsync(Guid zoneId, CancellationToken cancellationToken);

    /// <summary>Returns <c>true</c> if any zones already exist for the given lost-pet event.</summary>
    Task<bool> AnyForLostPetEventAsync(Guid lostPetEventId, CancellationToken cancellationToken);

    /// <summary>Persists a new zone.</summary>
    Task AddAsync(SearchZone zone, CancellationToken cancellationToken);

    /// <summary>Marks an existing zone as modified (EF Core change tracking).</summary>
    void Update(SearchZone zone);
}
