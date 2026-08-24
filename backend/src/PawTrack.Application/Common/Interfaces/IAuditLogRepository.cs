using PawTrack.Domain.Audit;

namespace PawTrack.Application.Common.Interfaces;

public interface IAuditLogRepository
{
    Task AddAsync(AuditLogEntry entry, CancellationToken ct = default);
    Task<IReadOnlyList<AuditLogEntry>> GetRecentAsync(int take = 100, CancellationToken ct = default);
    Task<IReadOnlyList<AuditLogEntry>> GetByEntityAsync(string entityType, string entityId, CancellationToken ct = default);
}
