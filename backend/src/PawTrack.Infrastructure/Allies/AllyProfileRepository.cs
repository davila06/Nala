using Microsoft.EntityFrameworkCore;
using PawTrack.Application.Common;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Allies;
using PawTrack.Infrastructure.Persistence;

namespace PawTrack.Infrastructure.Allies;

public sealed class AllyProfileRepository(PawTrackDbContext dbContext) : IAllyProfileRepository
{
    public Task<AllyProfile?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
        dbContext.AllyProfiles.FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);

    public Task<AllyProfile?> GetVerifiedByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
        dbContext.AllyProfiles.FirstOrDefaultAsync(
            x => x.UserId == userId && x.VerificationStatus == AllyVerificationStatus.Verified,
            cancellationToken);

    public async Task<IReadOnlyList<AllyProfile>> GetVerifiedCoveringPointAsync(
        double lat,
        double lng,
        CancellationToken cancellationToken = default)
    {
        var profiles = await dbContext.AllyProfiles
            .AsNoTracking()
            .Where(x => x.VerificationStatus == AllyVerificationStatus.Verified)
            .ToListAsync(cancellationToken);

        return profiles
            .Where(x => GeoHelper.DistanceMetres(lat, lng, x.CoverageLat, x.CoverageLng) <= x.CoverageRadiusMetres)
            .ToList()
            .AsReadOnly();
    }

    public async Task<IReadOnlyList<AllyProfile>> GetAllPendingAsync(CancellationToken cancellationToken = default) =>
        await dbContext.AllyProfiles
            .AsNoTracking()
            .Where(x => x.VerificationStatus == AllyVerificationStatus.Pending)
            .OrderBy(x => x.AppliedAt)
            .ToListAsync(cancellationToken);

    public Task AddAsync(AllyProfile profile, CancellationToken cancellationToken = default) =>
        dbContext.AllyProfiles.AddAsync(profile, cancellationToken).AsTask();

    public void Update(AllyProfile profile) =>
        dbContext.AllyProfiles.Update(profile);
}