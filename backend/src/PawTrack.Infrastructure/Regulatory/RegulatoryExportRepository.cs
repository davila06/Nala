using Microsoft.EntityFrameworkCore;
using PawTrack.Application.Regulatory.Interfaces;
using PawTrack.Domain.Regulatory;
using PawTrack.Infrastructure.Persistence;

namespace PawTrack.Infrastructure.Regulatory;

public sealed class RegulatoryExportRepository(PawTrackDbContext dbContext) : IRegulatoryExportRepository
{
    public Task<RegulatoryExport?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.RegulatoryExports.FindAsync([id], cancellationToken).AsTask();

    public Task<RegulatoryExport?> GetByIdempotencyKeyAsync(
        Guid requestedByUserId,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        dbContext.RegulatoryExports
            .FirstOrDefaultAsync(e => e.RequestedByUserId == requestedByUserId && e.IdempotencyKey == idempotencyKey, cancellationToken);

    public async Task<IReadOnlyList<RegulatoryExport>> GetByRequesterAsync(
        Guid requestedByUserId,
        int skip,
        int take,
        CancellationToken cancellationToken = default) =>
        await dbContext.RegulatoryExports.AsNoTracking()
            .Where(e => e.RequestedByUserId == requestedByUserId)
            .OrderByDescending(e => e.RequestedAt)
            .Skip(Math.Max(skip, 0))
            .Take(Math.Clamp(take, 1, 100))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<RegulatoryExport>> GetPendingAsync(
        int take,
        CancellationToken cancellationToken = default) =>
        await dbContext.RegulatoryExports
            .Where(e => e.Status == RegulatoryExportStatus.Requested)
            .OrderBy(e => e.RequestedAt)
            .Take(Math.Clamp(take, 1, 50))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<RegulatoryExport>> GetExpiredAsync(
        DateTimeOffset now,
        int take,
        CancellationToken cancellationToken = default) =>
        await dbContext.RegulatoryExports
            .Where(e => e.Status == RegulatoryExportStatus.Completed && e.ExpiresAt <= now)
            .OrderBy(e => e.ExpiresAt)
            .Take(Math.Clamp(take, 1, 100))
            .ToListAsync(cancellationToken);

    public async Task AddAsync(RegulatoryExport export, CancellationToken cancellationToken = default) =>
        await dbContext.RegulatoryExports.AddAsync(export, cancellationToken);

    public void Update(RegulatoryExport export) => dbContext.RegulatoryExports.Update(export);
}
