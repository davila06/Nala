using Microsoft.EntityFrameworkCore;
using PawTrack.Application.Common;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Locations;
using PawTrack.Infrastructure.Persistence;

namespace PawTrack.Infrastructure.Locations;

public sealed class UserLocationRepository(PawTrackDbContext dbContext) : IUserLocationRepository
{
    public Task<UserLocation?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
        dbContext.UserLocations
            .FirstOrDefaultAsync(u => u.UserId == userId, cancellationToken);

    public Task UpsertAsync(UserLocation userLocation, CancellationToken cancellationToken = default)
    {
        // First-time location updates arrive detached and must be inserted.
        // Existing tracked entities are already updated in-memory and saved by UnitOfWork.
        var entry = dbContext.Entry(userLocation);

        if (entry.State == EntityState.Detached)
            dbContext.UserLocations.Add(userLocation);

        return Task.CompletedTask;
    }

    public async Task<IReadOnlyList<UserLocation>> GetNearbyAlertSubscribersAsync(
        double lat,
        double lng,
        int radiusMetres,
        CancellationToken cancellationToken = default)
    {
        var (deltaLat, deltaLng) = GeoHelper.BoundingBoxDelta(lat, radiusMetres);

        var minLat = lat - deltaLat;
        var maxLat = lat + deltaLat;
        var minLng = lng - deltaLng;
        var maxLng = lng + deltaLng;

        // Step 1: cheap bounding-box filter in SQL.
        var candidates = await dbContext.UserLocations
            .AsNoTracking()
            .Where(u => u.ReceiveNearbyAlerts
                && u.Lat >= minLat && u.Lat <= maxLat
                && u.Lng >= minLng && u.Lng <= maxLng)
            .ToListAsync(cancellationToken);

        // Step 2: precise Haversine filter in memory on the small candidate set.
        return candidates
            .Where(u => GeoHelper.DistanceMetres(lat, lng, u.Lat, u.Lng) <= radiusMetres)
            .ToList()
            .AsReadOnly();
    }
}
