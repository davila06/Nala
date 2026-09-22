using Microsoft.EntityFrameworkCore;
using PawTrack.Application.Imports;
using PawTrack.Domain.Imports;
using PawTrack.Infrastructure.Persistence;

namespace PawTrack.Infrastructure.Imports;

public sealed class ImportJobRepository(PawTrackDbContext dbContext) : IImportJobRepository
{
    public Task<ImportJob?> GetByIdAsync(Guid tenantId, Guid jobId, CancellationToken cancellationToken = default) =>
        dbContext.ImportJobs.FirstOrDefaultAsync(
            job => job.TenantId == tenantId && job.Id == jobId,
            cancellationToken);

    public Task<ImportJob?> GetByIdempotencyKeyAsync(
        Guid tenantId,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        dbContext.ImportJobs.FirstOrDefaultAsync(
            job => job.TenantId == tenantId && job.IdempotencyKey == idempotencyKey,
            cancellationToken);

    public async Task AddAsync(ImportJob job, CancellationToken cancellationToken = default) =>
        await dbContext.ImportJobs.AddAsync(job, cancellationToken);

    public void Update(ImportJob job) => dbContext.ImportJobs.Update(job);
}
