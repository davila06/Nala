using Microsoft.EntityFrameworkCore;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Clinics;
using PawTrack.Infrastructure.Persistence;

namespace PawTrack.Infrastructure.Clinics;

public sealed class ClinicApiKeyRepository(PawTrackDbContext dbContext) : IClinicApiKeyRepository
{
    public async Task<ClinicApiKey?> GetByHashAsync(string keyHash, CancellationToken cancellationToken = default) =>
        await dbContext.ClinicApiKeys
            .AsTracking()
            .FirstOrDefaultAsync(k => k.KeyHash == keyHash && !k.IsRevoked, cancellationToken);

    public async Task<IReadOnlyList<ClinicApiKey>> GetForClinicAsync(
        Guid clinicId, CancellationToken cancellationToken = default) =>
        await dbContext.ClinicApiKeys
            .AsNoTracking()
            .Where(k => k.ClinicId == clinicId)
            .OrderByDescending(k => k.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(ClinicApiKey key, CancellationToken cancellationToken = default) =>
        await dbContext.ClinicApiKeys.AddAsync(key, cancellationToken);

    public void Update(ClinicApiKey key) =>
        dbContext.ClinicApiKeys.Update(key);
}
