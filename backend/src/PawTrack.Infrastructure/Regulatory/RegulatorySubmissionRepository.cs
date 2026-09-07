using Microsoft.EntityFrameworkCore;
using PawTrack.Application.Regulatory.Interfaces;
using PawTrack.Domain.Regulatory;
using PawTrack.Infrastructure.Persistence;

namespace PawTrack.Infrastructure.Regulatory;

public sealed class RegulatorySubmissionRepository(PawTrackDbContext dbContext) : IRegulatorySubmissionRepository
{
    public Task<RegulatorySubmission?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.RegulatorySubmissions.FindAsync([id], cancellationToken).AsTask();

    public Task<RegulatorySubmission?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default) =>
        dbContext.RegulatorySubmissions.AsNoTracking()
            .FirstOrDefaultAsync(s => s.IdempotencyKey == idempotencyKey, cancellationToken);

    public async Task AddAsync(RegulatorySubmission submission, CancellationToken cancellationToken = default) =>
        await dbContext.RegulatorySubmissions.AddAsync(submission, cancellationToken);

    public void Update(RegulatorySubmission submission) => dbContext.RegulatorySubmissions.Update(submission);
}
