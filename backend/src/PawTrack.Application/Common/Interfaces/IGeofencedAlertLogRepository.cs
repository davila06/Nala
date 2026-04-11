using PawTrack.Domain.Locations;

namespace PawTrack.Application.Common.Interfaces;

/// <summary>Tracks which users have already been geofenced-alerted for a given lost-pet event.</summary>
public interface IGeofencedAlertLogRepository
{
    /// <summary>
    /// Returns <c>true</c> if the user has already received a geofenced alert for this lost-pet
    /// event (regardless of when that alert was sent).
    /// </summary>
    Task<bool> HasBeenAlertedAsync(
        Guid userId,
        Guid lostPetEventId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists a new alert-log entry.
    /// Callers must follow with <see cref="IUnitOfWork.SaveChangesAsync"/> to commit.
    /// </summary>
    Task AddAsync(GeofencedAlertLog log, CancellationToken cancellationToken = default);
}
