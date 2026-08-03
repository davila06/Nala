using PawTrack.Domain.Municipalities;

namespace PawTrack.Application.Municipalities.Interfaces;

public interface IMunicipalProfileRepository
{
    Task<MunicipalityProfile?> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task AddAsync(MunicipalityProfile profile, CancellationToken ct = default);
    void Update(MunicipalityProfile profile);

    Task<IReadOnlyList<MunicipalityProfile>> GetAllActiveAsync(CancellationToken ct = default);
}
