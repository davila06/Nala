using Microsoft.EntityFrameworkCore;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Audit;
using PawTrack.Infrastructure.Persistence;

namespace PawTrack.Infrastructure.Audit;

public sealed class AuditLogRepository(PawTrackDbContext db) : IAuditLogRepository
{
    public async Task AddAsync(AuditLogEntry entry, CancellationToken ct = default) =>
        await db.AuditLog.AddAsync(entry, ct);

    public Task<int> CountByActionSinceAsync(
        AuditAction action,
        Guid actorId,
        DateTimeOffset since,
        CancellationToken ct = default) =>
        db.AuditLog.CountAsync(
            entry => entry.Action == action
                && entry.AdminUserId == actorId
                && entry.PerformedAt >= since,
            ct);

    public async Task<IReadOnlyList<AuditLogEntry>> GetFilteredAsync(
        AuditLogFilter filter,
        CancellationToken ct = default)
    {
        var query = db.AuditLog.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(filter.EntityType))
            query = query.Where(entry => entry.EntityType == filter.EntityType);
        if (!string.IsNullOrWhiteSpace(filter.EntityId))
            query = query.Where(entry => entry.EntityId == filter.EntityId);
        if (filter.ActorId.HasValue)
            query = query.Where(entry => entry.AdminUserId == filter.ActorId.Value);
        if (filter.From.HasValue)
            query = query.Where(entry => entry.PerformedAt >= filter.From.Value);
        if (filter.To.HasValue)
            query = query.Where(entry => entry.PerformedAt < filter.To.Value);

        return await query.OrderByDescending(entry => entry.PerformedAt)
            .Take(Math.Clamp(filter.Take, 1, 500))
            .ToListAsync(ct);
    }

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
