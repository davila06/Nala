namespace PawTrack.Domain.Broadcast;

/// <summary>
/// Lifecycle state of a single <see cref="BroadcastAttempt"/>.
/// Transitions: Pending → Sent | Failed | Skipped.
/// </summary>
public enum BroadcastStatus
{
    /// <summary>Queued but not yet dispatched.</summary>
    Pending,

    /// <summary>Successfully dispatched to the channel provider.</summary>
    Sent,

    /// <summary>Provider returned an error or a network failure occurred.</summary>
    Failed,

    /// <summary>
    /// Channel is disabled in configuration or lacks the required credentials.
    /// Not counted as a failure for alerting purposes.
    /// </summary>
    Skipped,
}
