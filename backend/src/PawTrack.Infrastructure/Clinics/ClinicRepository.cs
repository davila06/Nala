using Microsoft.EntityFrameworkCore;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Clinics;
using PawTrack.Infrastructure.Persistence;

namespace PawTrack.Infrastructure.Clinics;

public sealed class ClinicRepository(PawTrackDbContext dbContext) : IClinicRepository
{
    public async Task<Clinic?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await dbContext.Clinics
            .AsTracking()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Clinic>> GetByIdsAsync(
        IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
    {
        var idSet = ids.ToHashSet();
        return await dbContext.Clinics
            .AsNoTracking()
            .Where(c => idSet.Contains(c.Id))
            .ToListAsync(cancellationToken);
    }

    public async Task<Clinic?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
        await dbContext.Clinics
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);

    public async Task<Clinic?> GetByLicenseNumberAsync(
        string licenseNumber, CancellationToken cancellationToken = default) =>
        await dbContext.Clinics
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.LicenseNumber == licenseNumber.ToUpperInvariant(), cancellationToken);

    public async Task<IReadOnlyList<Clinic>> GetAllPendingAsync(CancellationToken cancellationToken = default) =>
        await dbContext.Clinics
            .AsNoTracking()
            .Where(c => c.Status == ClinicStatus.Pending)
            .OrderBy(c => c.RegisteredAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Clinic>> GetAllActiveAsync(CancellationToken cancellationToken = default) =>
        await dbContext.Clinics
            .AsNoTracking()
            .Where(c => c.Status == ClinicStatus.Active)
            .OrderByDescending(c => c.IsFeatured)
            .ThenBy(c => c.Name)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Clinic>> GetFeaturedNearAsync(
        double lat, double lng, double radiusKm,
        CancellationToken cancellationToken = default)
    {
        // Haversine approximation using bounding box pre-filter then distance sort.
        // For CR geography (small country) a bounding-box filter is accurate enough.
        double latDelta = radiusKm / 111.0;
        double lngDelta = radiusKm / (111.0 * Math.Cos(lat * Math.PI / 180.0));

        var minLat = (decimal)(lat - latDelta);
        var maxLat = (decimal)(lat + latDelta);
        var minLng = (decimal)(lng - lngDelta);
        var maxLng = (decimal)(lng + lngDelta);

        return await dbContext.Clinics
            .AsNoTracking()
            .Where(c => c.Status == ClinicStatus.Active
                     && c.IsFeatured
                     && c.Lat >= minLat && c.Lat <= maxLat
                     && c.Lng >= minLng && c.Lng <= maxLng)
            .OrderByDescending(c => c.IsFeatured)
            .ThenBy(c => c.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Clinic clinic, CancellationToken cancellationToken = default) =>
        await dbContext.Clinics.AddAsync(clinic, cancellationToken);

    public void Update(Clinic clinic) =>
        dbContext.Clinics.Update(clinic);
}

