using PawTrack.Domain.Incentives;

namespace PawTrack.Application.Common.Interfaces;

public interface IContributorScoreRepository
{
    /// <summary>Returns the score record for a user, or <c>null</c> if they have no reunifications yet.</summary>
    Task<ContributorScore?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the top <paramref name="take"/> contributors ordered by <c>ReunificationCount</c> descending.
    /// </summary>
    Task<IReadOnlyList<ContributorScore>> GetLeaderboardAsync(
        int take,
        CancellationToken cancellationToken = default);

    Task AddAsync(ContributorScore score, CancellationToken cancellationToken = default);
    void Update(ContributorScore score);
}
