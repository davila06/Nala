using Microsoft.EntityFrameworkCore;
using PawTrack.Application.Municipalities.Interfaces;
using PawTrack.Domain.Municipalities;
using PawTrack.Infrastructure.Persistence;

namespace PawTrack.Infrastructure.Municipalities;

public sealed class MunicipalProfileRepository(PawTrackDbContext dbContext) : IMunicipalProfileRepository
{
    public Task<MunicipalityProfile?> GetByUserIdAsync(Guid userId, CancellationToken ct = default) =>
        dbContext.MunicipalityProfiles
            .FirstOrDefaultAsync(p => p.UserId == userId, ct);

    public async Task AddAsync(MunicipalityProfile profile, CancellationToken ct = default) =>
        await dbContext.MunicipalityProfiles.AddAsync(profile, ct);

    public void Update(MunicipalityProfile profile) =>
        dbContext.MunicipalityProfiles.Update(profile);

    public Task<IReadOnlyList<MunicipalityProfile>> GetAllActiveAsync(CancellationToken ct = default) =>
        dbContext.MunicipalityProfiles
            .Where(p => p.IsActive)
            .OrderBy(p => p.Canton)
            .ToListAsync(ct)
            .ContinueWith<IReadOnlyList<MunicipalityProfile>>(t => t.Result, ct);
}
