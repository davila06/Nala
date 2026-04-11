namespace PawTrack.Domain.Incentives;

/// <summary>
/// Recognition tier awarded to a pet owner based on cumulative pet reunifications.
/// Thresholds are purposely achievable so even a single successful reunification
/// carries meaning in the community.
/// </summary>
public enum ContributorBadge
{
    /// <summary>No reunifications recorded yet.</summary>
    None = 0,

    /// <summary>1 successful reunification.</summary>
    Helper = 1,

    /// <summary>3 cumulative reunifications.</summary>
    Rescuer = 3,

    /// <summary>10 cumulative reunifications.</summary>
    Guardian = 10,

    /// <summary>25+ cumulative reunifications — top community contributor.</summary>
    Legend = 25,
}
