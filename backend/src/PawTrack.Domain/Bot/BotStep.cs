namespace PawTrack.Domain.Bot;

/// <summary>
/// Represents the current step in the WhatsApp conversation flow.
/// Transitions: Idle → AwaitingPetName → AwaitingLastSeenTime → AwaitingLocation → Completed.
/// </summary>
public enum BotStep
{
    /// <summary>Session just created; bot sent welcome + first question.</summary>
    AwaitingPetName = 0,

    /// <summary>Pet name collected; bot asked when the pet was last seen.</summary>
    AwaitingLastSeenTime = 1,

    /// <summary>Time collected; bot asked for last-seen location.</summary>
    AwaitingLocation = 2,

    /// <summary>Report created and link sent. Terminal state.</summary>
    Completed = 3,

    /// <summary>Session expired without completing the flow.</summary>
    Expired = 4,
}
