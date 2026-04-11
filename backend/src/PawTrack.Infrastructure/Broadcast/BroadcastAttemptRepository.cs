using Microsoft.EntityFrameworkCore;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Broadcast;
using PawTrack.Infrastructure.Persistence;

namespace PawTrack.Infrastructure.Broadcast;

public sealed class BroadcastAttemptRepository(PawTrackDbContext dbContext)
    : IBroadcastAttemptRepository
{
    public async Task AddAsync(BroadcastAttempt attempt, CancellationToken cancellationToken = default) =>
        await dbContext.BroadcastAttempts.AddAsync(attempt, cancellationToken);

    public async Task<IReadOnlyList<BroadcastAttempt>> GetByLostEventIdAsync(
        Guid lostPetEventId, CancellationToken cancellationToken = default) =>
        await dbContext.BroadcastAttempts
            .Where(a => a.LostPetEventId == lostPetEventId)
            .OrderBy(a => a.Channel)
            .ThenByDescending(a => a.CreatedAt)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

    public async Task<BroadcastAttempt?> GetByIdAsync(
        Guid id, CancellationToken cancellationToken = default) =>
        await dbContext.BroadcastAttempts
            .AsTracking()
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
}
