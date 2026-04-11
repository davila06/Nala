using PawTrack.Domain.Broadcast;

namespace PawTrack.Application.Common.Interfaces;

public interface IBroadcastAttemptRepository
{
    Task AddAsync(BroadcastAttempt attempt, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BroadcastAttempt>> GetByLostEventIdAsync(Guid lostPetEventId, CancellationToken cancellationToken = default);
    /// <summary>Loads a single attempt by its ID so the orchestrator can update its state.</summary>
    Task<BroadcastAttempt?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
