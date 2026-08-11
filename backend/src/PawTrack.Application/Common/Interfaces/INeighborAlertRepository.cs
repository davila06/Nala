using PawTrack.Domain.Locations;

namespace PawTrack.Application.Common.Interfaces;

public interface INeighborAlertRepository
{
    Task<NeighborAlert?> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task AddAsync(NeighborAlert alert, CancellationToken ct = default);
    void Update(NeighborAlert alert);

    /// <summary>Returns all active neighbors within <paramref name="radiusMeters"/> of the given coordinates.</summary>
    Task<IReadOnlyList<NeighborAlert>> GetActiveInRadiusAsync(
        double lat, double lng, int radiusMeters, CancellationToken ct = default);

    /// <summary>Returns the count of active neighbors within the given radius (used for the ReportLost UX hint).</summary>
    Task<int> CountActiveInRadiusAsync(double lat, double lng, int radiusMeters, CancellationToken ct = default);
}
