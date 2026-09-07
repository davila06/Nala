using PawTrack.Domain.AnimalWelfare;

namespace PawTrack.Application.AnimalWelfare.Interfaces;

public interface IAnimalWelfareAuditRepository
{
    Task AddAsync(AnimalWelfareCaseAuditLog log, CancellationToken cancellationToken = default);
    Task AddNoteAsync(AnimalWelfareCaseNote note, CancellationToken cancellationToken = default);
    Task AddReferralAsync(AnimalWelfareReferral referral, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AnimalWelfareCaseAuditLog>> GetAuditByCaseIdAsync(Guid caseId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AnimalWelfareCaseNote>> GetNotesByCaseIdAsync(Guid caseId, CancellationToken cancellationToken = default);
}
