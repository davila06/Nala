using PawTrack.Domain.Regulatory;

namespace PawTrack.Application.Regulatory.Interfaces;

public interface IRegulatorySubmissionRepository
{
    Task<RegulatorySubmission?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<RegulatorySubmission?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default);
    Task AddAsync(RegulatorySubmission submission, CancellationToken cancellationToken = default);
    void Update(RegulatorySubmission submission);
}
