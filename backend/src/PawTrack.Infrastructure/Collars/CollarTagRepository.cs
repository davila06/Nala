using Microsoft.EntityFrameworkCore;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Collars;
using PawTrack.Infrastructure.Persistence;

namespace PawTrack.Infrastructure.Collars;

public sealed class CollarTagRepository(PawTrackDbContext dbContext) : ICollarTagRepository
{
    public Task<CollarTag?> GetBySerialAsync(string serial, CancellationToken cancellationToken = default) =>
        dbContext.CollarTags.FirstOrDefaultAsync(t => t.Serial == serial, cancellationToken);

    public async Task<IReadOnlyList<CollarTag>> GetAllAsync(int skip, int take, CancellationToken cancellationToken = default) =>
        await dbContext.CollarTags
            .OrderByDescending(t => t.ManufacturedAt)
            .Skip(skip).Take(take)
            .ToListAsync(cancellationToken);

    public Task<int> CountAsync(CancellationToken cancellationToken = default) =>
        dbContext.CollarTags.CountAsync(cancellationToken);

    public async Task AddAsync(CollarTag tag, CancellationToken cancellationToken = default) =>
        await dbContext.CollarTags.AddAsync(tag, cancellationToken);

    public void Update(CollarTag tag) =>
        dbContext.CollarTags.Update(tag);

    public async Task<(IReadOnlyList<CollarTag> Items, int Total)> SearchAsync(
        string? serialContains,
        CollarTagStatus? status,
        DateTimeOffset? soldAfter,
        DateTimeOffset? soldBefore,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.CollarTags.AsQueryable();

        if (!string.IsNullOrWhiteSpace(serialContains))
            query = query.Where(t => t.Serial.Contains(serialContains.ToUpperInvariant()));
        if (status is not null)
            query = query.Where(t => t.Status == status.Value);
        if (soldAfter is not null)
            query = query.Where(t => t.SoldAt != null && t.SoldAt >= soldAfter.Value);
        if (soldBefore is not null)
            query = query.Where(t => t.SoldAt != null && t.SoldAt <= soldBefore.Value);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(t => t.ManufacturedAt)
            .Skip(skip).Take(take)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public async Task<CollarTagMetricsDto> GetMetricsAsync(CancellationToken cancellationToken = default)
    {
        var total = await dbContext.CollarTags.CountAsync(cancellationToken);
        var unactivated = await dbContext.CollarTags.CountAsync(t => t.Status == CollarTagStatus.Unactivated, cancellationToken);
        var activated = await dbContext.CollarTags.CountAsync(t => t.Status == CollarTagStatus.Activated, cancellationToken);
        var deactivated = await dbContext.CollarTags.CountAsync(t => t.Status == CollarTagStatus.Deactivated, cancellationToken);

        var thirtyDaysAgo = DateTimeOffset.UtcNow.AddDays(-30);
        var soldLast30Days = await dbContext.CollarTags
            .CountAsync(t => t.SoldAt != null && t.SoldAt >= thirtyDaysAgo, cancellationToken);

        var ninetyDaysAgo = DateTimeOffset.UtcNow.AddDays(-90);
        var deadInventory = await dbContext.CollarTags
            .CountAsync(t => t.Status == CollarTagStatus.Unactivated && t.SoldAt != null && t.SoldAt <= ninetyDaysAgo, cancellationToken);

        return new CollarTagMetricsDto(total, unactivated, activated, deactivated, soldLast30Days, deadInventory);
    }
}

public sealed class CollarDeviceCredentialRepository(PawTrackDbContext dbContext) : ICollarDeviceCredentialRepository
{
    public Task<CollarDeviceCredential?> GetActiveByHashAsync(string keyHash, CancellationToken cancellationToken = default) =>
        dbContext.CollarDeviceCredentials
            .FirstOrDefaultAsync(c => c.KeyHash == keyHash && c.RevokedAt == null, cancellationToken);

    public async Task<IReadOnlyList<CollarDeviceCredential>> GetForCollarAsync(Guid collarId, CancellationToken cancellationToken = default) =>
        await dbContext.CollarDeviceCredentials
            .Where(c => c.CollarId == collarId)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(CollarDeviceCredential credential, CancellationToken cancellationToken = default) =>
        await dbContext.CollarDeviceCredentials.AddAsync(credential, cancellationToken);

    public void Update(CollarDeviceCredential credential) =>
        dbContext.CollarDeviceCredentials.Update(credential);
}
