using PawTrack.Domain.Allies;

namespace PawTrack.Application.Common.Interfaces;

public interface IAllyProfileRepository
{
    Task<AllyProfile?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<AllyProfile?> GetVerifiedByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AllyProfile>> GetVerifiedCoveringPointAsync(
        double lat,
        double lng,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AllyProfile>> GetAllPendingAsync(CancellationToken cancellationToken = default);
    Task AddAsync(AllyProfile profile, CancellationToken cancellationToken = default);
    void Update(AllyProfile profile);
}