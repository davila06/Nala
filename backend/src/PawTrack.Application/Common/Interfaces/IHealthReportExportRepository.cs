using PawTrack.Domain.Medical;

namespace PawTrack.Application.Common.Interfaces;

public interface IHealthReportExportRepository
{
    Task AddAsync(HealthReportExport export, CancellationToken cancellationToken = default);
    Task<HealthReportExport?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<HealthReportExport?> GetReusableAsync(Guid requestedByUserId, Guid petId, DateTimeOffset now, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<HealthReportExport>> GetQueuedAsync(int take, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<HealthReportExport>> GetStaleProcessingAsync(DateTimeOffset staleBefore, int take, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<HealthReportExport>> GetExpiredAsync(DateTimeOffset now, int take, CancellationToken cancellationToken = default);
    void Update(HealthReportExport export);
}
