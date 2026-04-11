namespace PawTrack.Domain.Safety;

/// <summary>
/// Overall suspicion level assigned to a target user based on the volume
/// of fraud reports received in a rolling window.
/// </summary>
public enum FraudSuspicionLevel
{
    /// <summary>No pattern detected; ≤ 1 report.</summary>
    None = 0,

    /// <summary>2–3 reports in the rolling window.</summary>
    Elevated = 1,

    /// <summary>4–5 reports in the rolling window.</summary>
    High = 2,

    /// <summary>≥ 6 reports — escalate to admin immediately.</summary>
    Critical = 3,
}
