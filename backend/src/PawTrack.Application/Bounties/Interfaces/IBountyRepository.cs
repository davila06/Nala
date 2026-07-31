using PawTrack.Domain.Bounties;

namespace PawTrack.Application.Bounties.Interfaces;

public interface IBountyRepository
{
    Task<Bounty?> GetByLostEventAsync(Guid lostEventId, CancellationToken cancellationToken = default);
    Task<Bounty?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Bounty?> GetByDepositReferenceAsync(string reference, CancellationToken cancellationToken = default);
    Task AddAsync(Bounty bounty, CancellationToken cancellationToken = default);
    void Update(Bounty bounty);
}
