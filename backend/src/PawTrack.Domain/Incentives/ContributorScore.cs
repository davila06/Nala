namespace PawTrack.Domain.Incentives;

/// <summary>
/// Aggregate lifetime contributor record for a registered user.
/// Updated whenever one of the user's lost-pet reports transitions to
/// <c>Reunited</c>. One row per user — created on first reunification.
/// </summary>
public sealed class ContributorScore
{
    private ContributorScore() { } // EF Core

    /// <summary>Same value as <c>User.Id</c> — no FK to keep modules independent.</summary>
    public Guid UserId { get; private set; }

    /// <summary>Display name snapshotted at the time of the last update.</summary>
    public string OwnerName { get; private set; } = string.Empty;

    /// <summary>Total lifetime count of pets successfully reunited.</summary>
    public int ReunificationCount { get; private set; }

    /// <summary>Current badge tier derived from <see cref="ReunificationCount"/>.</summary>
    public ContributorBadge Badge { get; private set; }

    /// <summary>Simple point total: 100 pts per reunification.</summary>
    public int TotalPoints { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    // ── Factory ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Creates the first-time score record for a user after their first reunification.
    /// </summary>
    public static ContributorScore Create(Guid userId, string ownerName)
    {
        var now = DateTimeOffset.UtcNow;
        var score = new ContributorScore
        {
            UserId = userId,
            OwnerName = ownerName,
            ReunificationCount = 0,
            Badge = ContributorBadge.None,
            TotalPoints = 0,
            CreatedAt = now,
            UpdatedAt = now,
        };
        return score;
    }

    // ── Behaviour ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Records one successful reunification and recalculates badge and points.
    /// Snapshot the current owner name so the leaderboard stays accurate even if
    /// the user renames.
    /// </summary>
    public void RecordReunification(string ownerName)
    {
        OwnerName = ownerName;
        ReunificationCount++;
        TotalPoints += 100;
        Badge = CalculateBadge(ReunificationCount);
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private static ContributorBadge CalculateBadge(int count) => count switch
    {
        >= 25 => ContributorBadge.Legend,
        >= 10 => ContributorBadge.Guardian,
        >= 3  => ContributorBadge.Rescuer,
        >= 1  => ContributorBadge.Helper,
        _     => ContributorBadge.None,
    };
}
