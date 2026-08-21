namespace PawTrack.Domain.Adoptions;

public enum ApplicationStatus
{
    Pending,
    UnderReview,
    Approved,
    Rejected,
    Withdrawn,
}

public sealed class AdoptionApplication
{
    private AdoptionApplication() { } // EF Core

    public Guid Id { get; private set; }
    public Guid AdoptablePetId { get; private set; }
    /// <summary>FK to Auth.Users — the applicant (Role = Owner).</summary>
    public Guid ApplicantUserId { get; private set; }
    public string ApplicantNote { get; private set; } = string.Empty;
    public ApplicationStatus Status { get; private set; }
    public string? ReviewNote { get; private set; }
    public DateTimeOffset AppliedAt { get; private set; }
    public DateTimeOffset? ReviewedAt { get; private set; }

    // ── Factory ───────────────────────────────────────────────────────────────

    public static AdoptionApplication Create(
        Guid adoptablePetId,
        Guid applicantUserId,
        string applicantNote) => new()
        {
            Id = Guid.CreateVersion7(),
            AdoptablePetId = adoptablePetId,
            ApplicantUserId = applicantUserId,
            ApplicantNote = applicantNote.Trim(),
            Status = ApplicationStatus.Pending,
            AppliedAt = DateTimeOffset.UtcNow,
        };

    // ── Behaviour ─────────────────────────────────────────────────────────────

    public void StartReview()
    {
        Status = ApplicationStatus.UnderReview;
        ReviewedAt = DateTimeOffset.UtcNow;
    }

    public void Approve(string? note = null)
    {
        Status = ApplicationStatus.Approved;
        ReviewNote = note?.Trim();
        ReviewedAt = DateTimeOffset.UtcNow;
    }

    public void Reject(string? note = null)
    {
        Status = ApplicationStatus.Rejected;
        ReviewNote = note?.Trim();
        ReviewedAt = DateTimeOffset.UtcNow;
    }

    public void Withdraw()
    {
        if (Status is not (ApplicationStatus.Pending or ApplicationStatus.UnderReview))
            throw new InvalidOperationException("Only pending or under-review applications can be withdrawn.");
        Status = ApplicationStatus.Withdrawn;
    }
}
