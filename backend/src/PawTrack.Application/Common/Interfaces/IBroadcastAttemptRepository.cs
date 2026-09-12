using PawTrack.Domain.Broadcast;

namespace PawTrack.Application.Common.Interfaces;

public interface IBroadcastAttemptRepository
{
    Task AddAsync(BroadcastAttempt attempt, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BroadcastAttempt>> GetByLostEventIdAsync(Guid lostPetEventId, CancellationToken cancellationToken = default);
    Task<BroadcastAttempt?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<BroadcastAttempt?> GetByExternalIdAsync(string externalId, CancellationToken cancellationToken = default);
    void Update(BroadcastAttempt attempt);
}
