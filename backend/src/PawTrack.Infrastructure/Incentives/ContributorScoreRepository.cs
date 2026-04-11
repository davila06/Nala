using Microsoft.EntityFrameworkCore;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Incentives;
using PawTrack.Infrastructure.Persistence;

namespace PawTrack.Infrastructure.Incentives;

public sealed class ContributorScoreRepository(PawTrackDbContext db) : IContributorScoreRepository
{
    public Task<ContributorScore?> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default) =>
        db.ContributorScores
            .AsTracking()
            .FirstOrDefaultAsync(s => s.UserId == userId, cancellationToken);

    public async Task<IReadOnlyList<ContributorScore>> GetLeaderboardAsync(
        int take,
        CancellationToken cancellationToken = default)
    {
        var results = await db.ContributorScores
            .AsNoTracking()
            .OrderByDescending(s => s.ReunificationCount)
            .ThenByDescending(s => s.UpdatedAt)
            .Take(take)
            .ToListAsync(cancellationToken);

        return results;
    }

    public Task AddAsync(ContributorScore score, CancellationToken cancellationToken = default)
    {
        return db.ContributorScores.AddAsync(score, cancellationToken).AsTask();
    }

    public void Update(ContributorScore score) =>
        db.ContributorScores.Update(score);
}
