using PawTrack.Domain.Locations;

namespace PawTrack.Application.Common.Interfaces;

public interface IUserLocationRepository
{
    /// <summary>Returns the stored location for the given user, or <c>null</c> if none exists.</summary>
    Task<UserLocation?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Inserts or replaces the user's location record (upsert semantics — one row per user).
    /// Callers must follow with <see cref="IUnitOfWork.SaveChangesAsync"/> to persist.
    /// </summary>
    Task UpsertAsync(UserLocation userLocation, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns opted-in users whose last known location is within <paramref name="radiusMetres"/>
    /// metres of the given coordinates.
    /// <para>
    /// Implementation applies a bounding-box pre-filter in SQL, then Haversine precision in memory.
    /// </para>
    /// </summary>
    Task<IReadOnlyList<UserLocation>> GetNearbyAlertSubscribersAsync(
        double lat,
        double lng,
        int radiusMetres,
        CancellationToken cancellationToken = default);
}
