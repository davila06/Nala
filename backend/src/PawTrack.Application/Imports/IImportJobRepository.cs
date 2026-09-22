using PawTrack.Domain.Imports;

namespace PawTrack.Application.Imports;

public interface IImportJobRepository
{
    Task<ImportJob?> GetByIdAsync(Guid tenantId, Guid jobId, CancellationToken cancellationToken = default);
    Task<ImportJob?> GetByIdempotencyKeyAsync(Guid tenantId, string idempotencyKey, CancellationToken cancellationToken = default);
    Task AddAsync(ImportJob job, CancellationToken cancellationToken = default);
    void Update(ImportJob job);
}
