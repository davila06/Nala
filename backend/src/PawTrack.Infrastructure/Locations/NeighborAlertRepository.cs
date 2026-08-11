using Microsoft.EntityFrameworkCore;
using PawTrack.Application.Common;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Locations;
using PawTrack.Infrastructure.Persistence;

namespace PawTrack.Infrastructure.Locations;

public sealed class NeighborAlertRepository(PawTrackDbContext db) : INeighborAlertRepository
{
    public Task<NeighborAlert?> GetByUserIdAsync(Guid userId, CancellationToken ct = default) =>
        db.NeighborAlerts.FirstOrDefaultAsync(n => n.UserId == userId, ct);

    public async Task AddAsync(NeighborAlert alert, CancellationToken ct = default) =>
        await db.NeighborAlerts.AddAsync(alert, ct);

    public void Update(NeighborAlert alert) => db.NeighborAlerts.Update(alert);

    public async Task<IReadOnlyList<NeighborAlert>> GetActiveInRadiusAsync(
        double lat, double lng, int radiusMeters, CancellationToken ct = default)
    {
        var (deltaLat, deltaLng) = GeoHelper.BoundingBoxDelta(lat, radiusMeters);
        var candidates = await db.NeighborAlerts.AsNoTracking()
            .Where(n => n.IsActive
                && n.Lat >= (decimal)(lat - deltaLat) && n.Lat <= (decimal)(lat + deltaLat)
                && n.Lng >= (decimal)(lng - deltaLng) && n.Lng <= (decimal)(lng + deltaLng))
            .ToListAsync(ct);

        return candidates
            .Where(n => GeoHelper.DistanceMetres(lat, lng, (double)n.Lat, (double)n.Lng) <= n.RadiusMeters)
            .ToList()
            .AsReadOnly();
    }

    public async Task<int> CountActiveInRadiusAsync(
        double lat, double lng, int radiusMeters, CancellationToken ct = default) =>
        (await GetActiveInRadiusAsync(lat, lng, radiusMeters, ct)).Count;
}
