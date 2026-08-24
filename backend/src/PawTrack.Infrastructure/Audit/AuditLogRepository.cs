using Microsoft.EntityFrameworkCore;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Audit;
using PawTrack.Infrastructure.Persistence;

namespace PawTrack.Infrastructure.Audit;

public sealed class AuditLogRepository(PawTrackDbContext db) : IAuditLogRepository
{
    public async Task AddAsync(AuditLogEntry entry, CancellationToken ct = default) =>
        await db.AuditLog.AddAsync(entry, ct);

    public async Task<IReadOnlyList<AuditLogEntry>> GetRecentAsync(int take = 100, CancellationToken ct = default) =>
        await db.AuditLog.AsNoTracking()
            .OrderByDescending(a => a.PerformedAt)
            .Take(take)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<AuditLogEntry>> GetByEntityAsync(
        string entityType, string entityId, CancellationToken ct = default) =>
        await db.AuditLog.AsNoTracking()
            .Where(a => a.EntityType == entityType && a.EntityId == entityId)
            .OrderByDescending(a => a.PerformedAt)
            .ToListAsync(ct);
}
