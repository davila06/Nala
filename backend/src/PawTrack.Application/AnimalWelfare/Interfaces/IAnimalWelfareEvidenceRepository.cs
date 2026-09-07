using PawTrack.Domain.AnimalWelfare;

namespace PawTrack.Application.AnimalWelfare.Interfaces;

public interface IAnimalWelfareEvidenceRepository
{
    Task<AnimalWelfareEvidence?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AnimalWelfareEvidence>> GetByCaseIdAsync(Guid caseId, CancellationToken cancellationToken = default);
    Task AddAsync(AnimalWelfareEvidence evidence, CancellationToken cancellationToken = default);
}
