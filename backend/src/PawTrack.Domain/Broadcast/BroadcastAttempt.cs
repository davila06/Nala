namespace PawTrack.Domain.Broadcast;

/// <summary>
/// Records the outcome of one channel attempt within a lost-pet broadcast campaign.
/// One <see cref="BroadcastAttempt"/> is created per channel per broadcast trigger.
/// The aggregate is intentionally flat (no parent root) to allow efficient
/// per-event queries without loading every attempt in memory.
/// </summary>
public sealed class BroadcastAttempt
{
    private BroadcastAttempt() { } // EF Core

    public Guid Id { get; private set; }

    /// <summary>The <c>LostPetEvent.Id</c> that originated this broadcast.</summary>
    public Guid LostPetEventId { get; private set; }

    public BroadcastChannel Channel { get; private set; }
    public BroadcastStatus Status { get; private set; }

    /// <summary>
    /// Provider-level message identifier (e.g. Twilio SID, Telegram message_id).
    /// Null when status is <see cref="BroadcastStatus.Failed"/> or
    /// <see cref="BroadcastStatus.Skipped"/>.
    /// </summary>
    public string? ExternalId { get; private set; }

    /// <summary>
    /// Short tracking URL generated for this attempt (e.g. pawtrack.cr/t/abc123).
    /// Used to measure clicks back to the pet profile.
    /// </summary>
    public string? TrackingUrl { get; private set; }

    /// <summary>Number of times the tracking link was followed. Updated externally.</summary>
    public int TrackingClicks { get; private set; }

    /// <summary>Null on success; contains the sanitised provider error on failure.</summary>
    public string? ErrorMessage { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? SentAt { get; private set; }

    // ── Factory ──────────────────────────────────────────────────────────────

    public static BroadcastAttempt CreatePending(
        Guid lostPetEventId,
        BroadcastChannel channel,
        string? trackingUrl)
    {
        return new BroadcastAttempt
        {
            Id = Guid.CreateVersion7(),
            LostPetEventId = lostPetEventId,
            Channel = channel,
            Status = BroadcastStatus.Pending,
            TrackingUrl = trackingUrl,
            TrackingClicks = 0,
            CreatedAt = DateTimeOffset.UtcNow,
        };
    }

    // ── State transitions ─────────────────────────────────────────────────────

    public void MarkSent(string? externalId)
    {
        Status = BroadcastStatus.Sent;
        ExternalId = externalId;
        SentAt = DateTimeOffset.UtcNow;
        ErrorMessage = null;
    }

    public void MarkFailed(string errorMessage)
    {
        Status = BroadcastStatus.Failed;
        // Truncate to keep the column bounded (see EF config).
        ErrorMessage = errorMessage.Length > 500
            ? string.Concat(errorMessage.AsSpan(0, 497), "...")
            : errorMessage;
        SentAt = null;
    }

    public void MarkSkipped(string reason)
    {
        Status = BroadcastStatus.Skipped;
        ErrorMessage = reason.Length > 500
            ? string.Concat(reason.AsSpan(0, 497), "...")
            : reason;
    }

    public void IncrementClicks() => TrackingClicks++;
}
