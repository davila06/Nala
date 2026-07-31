using Microsoft.EntityFrameworkCore;
using PawTrack.Application.Bounties.Interfaces;
using PawTrack.Domain.Bounties;
using PawTrack.Infrastructure.Persistence;

namespace PawTrack.Infrastructure.Bounties;

public sealed class BountyRepository(PawTrackDbContext dbContext) : IBountyRepository
{
    public Task<Bounty?> GetByLostEventAsync(Guid lostEventId, CancellationToken cancellationToken = default) =>
        dbContext.Bounties
            .OrderByDescending(b => b.CreatedAt)
            .FirstOrDefaultAsync(b => b.LostPetEventId == lostEventId, cancellationToken);

    public Task<Bounty?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.Bounties.FindAsync([id], cancellationToken).AsTask();

    public Task<Bounty?> GetByDepositReferenceAsync(string reference, CancellationToken cancellationToken = default) =>
        dbContext.Bounties.FirstOrDefaultAsync(b => b.DepositReference == reference, cancellationToken);

    public async Task AddAsync(Bounty bounty, CancellationToken cancellationToken = default) =>
        await dbContext.Bounties.AddAsync(bounty, cancellationToken);

    public void Update(Bounty bounty) =>
        dbContext.Bounties.Update(bounty);
}
