namespace PawTrack.Domain.Safety;

/// <summary>
/// An individual fraud or scam attempt report submitted by a user or anonymous reporter.
/// Aggregate counts over a rolling 7-day window drive the <see cref="FraudSuspicionLevel"/>
/// assigned to the target.
/// </summary>
public sealed class FraudReport
{
    private FraudReport() { } // EF Core

    public Guid  Id               { get; private set; }

    /// <summary>Null when the reporter is anonymous (unauthenticated).</summary>
    public Guid? ReporterUserId   { get; private set; }

    /// <summary>SHA-256 hex of the reporter's IP address, used for anonymous rate-limiting.</summary>
    public string ReporterIpHash  { get; private set; } = string.Empty;

    public FraudContext Context   { get; private set; }

    /// <summary>
    /// The <c>LostPetEventId</c> or <c>ChatThreadId</c> that the report is associated with.
    /// Null when context is <see cref="FraudContext.Other"/>.
    /// </summary>
    public Guid?  RelatedEntityId { get; private set; }

    /// <summary>The user being accused. Null when the target's identity is unknown.</summary>
    public Guid?  TargetUserId    { get; private set; }

    /// <summary>Optional free-text description of what happened (max 500 chars).</summary>
    public string? Description    { get; private set; }

    public DateTimeOffset ReportedAt { get; private set; }

    /// <summary>Computed at creation time from the pattern detector.</summary>
    public FraudSuspicionLevel SuspicionLevel { get; private set; }

    // ── Factory ────────────────────────────────────────────────────────────────

    public static FraudReport Create(
        Guid?               reporterUserId,
        string              reporterIpHash,
        FraudContext        context,
        Guid?               relatedEntityId,
        Guid?               targetUserId,
        string?             description,
        FraudSuspicionLevel suspicionLevel) =>
        new()
        {
            Id              = Guid.CreateVersion7(),
            ReporterUserId  = reporterUserId,
            ReporterIpHash  = reporterIpHash,
            Context         = context,
            RelatedEntityId = relatedEntityId,
            TargetUserId    = targetUserId,
            Description     = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            ReportedAt      = DateTimeOffset.UtcNow,
            SuspicionLevel  = suspicionLevel,
        };
}
