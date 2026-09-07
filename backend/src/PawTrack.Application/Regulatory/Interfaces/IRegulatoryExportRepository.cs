using PawTrack.Domain.Regulatory;

namespace PawTrack.Application.Regulatory.Interfaces;

public interface IRegulatoryExportRepository
{
    Task<RegulatoryExport?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<RegulatoryExport?> GetByIdempotencyKeyAsync(Guid requestedByUserId, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RegulatoryExport>> GetByRequesterAsync(Guid requestedByUserId, int skip, int take, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RegulatoryExport>> GetPendingAsync(int take, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RegulatoryExport>> GetExpiredAsync(DateTimeOffset now, int take, CancellationToken cancellationToken = default);
    Task AddAsync(RegulatoryExport export, CancellationToken cancellationToken = default);
    void Update(RegulatoryExport export);
}
