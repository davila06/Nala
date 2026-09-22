using PawTrack.Domain.Audit;

namespace PawTrack.Application.Common.Interfaces;

public interface IAuditLogRepository
{
    Task AddAsync(AuditLogEntry entry, CancellationToken ct = default);
    Task<IReadOnlyList<AuditLogEntry>> GetFilteredAsync(AuditLogFilter filter, CancellationToken ct = default);
    Task<int> CountByActionSinceAsync(
        AuditAction action,
        Guid actorId,
        DateTimeOffset since,
        CancellationToken ct = default);
    Task<IReadOnlyList<AuditLogEntry>> GetRecentAsync(int take = 100, CancellationToken ct = default);
    Task<IReadOnlyList<AuditLogEntry>> GetByEntityAsync(string entityType, string entityId, CancellationToken ct = default);
}

public sealed record AuditLogFilter(
    string? EntityType,
    string? EntityId,
    Guid? ActorId,
    DateTimeOffset? From,
    DateTimeOffset? To,
    int Take);
