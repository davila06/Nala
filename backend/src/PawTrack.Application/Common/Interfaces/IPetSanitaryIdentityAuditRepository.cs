using PawTrack.Domain.Pets;

namespace PawTrack.Application.Common.Interfaces;

public interface IPetSanitaryIdentityAuditRepository
{
    Task AddAsync(PetSanitaryIdentityAuditLog log, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PetSanitaryIdentityAuditLog>> GetByPetIdAsync(Guid petId, int take, CancellationToken cancellationToken = default);
}
