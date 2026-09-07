using Microsoft.EntityFrameworkCore;
using PawTrack.Application.AnimalWelfare.Interfaces;
using PawTrack.Domain.AnimalWelfare;
using PawTrack.Infrastructure.Persistence;

namespace PawTrack.Infrastructure.AnimalWelfare;

public sealed class AnimalWelfareEvidenceRepository(PawTrackDbContext dbContext) : IAnimalWelfareEvidenceRepository
{
    public Task<AnimalWelfareEvidence?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.AnimalWelfareEvidence.FindAsync([id], cancellationToken).AsTask();

    public async Task<IReadOnlyList<AnimalWelfareEvidence>> GetByCaseIdAsync(Guid caseId, CancellationToken cancellationToken = default) =>
        await dbContext.AnimalWelfareEvidence.AsNoTracking()
            .Where(e => e.CaseId == caseId)
            .OrderByDescending(e => e.UploadedAt)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(AnimalWelfareEvidence evidence, CancellationToken cancellationToken = default) =>
        await dbContext.AnimalWelfareEvidence.AddAsync(evidence, cancellationToken);
}
