namespace PawTrack.Domain.AnimalWelfare;

public enum WelfareAuditAction
{
    CaseReported,
    EvidenceUploaded,
    TriageStarted,
    SeverityChanged,
    Assigned,
    Referred,
    StatusChanged,
    Resolved,
    Dismissed,
    ClosedNoAction,
    DocumentDownloaded,
    NoteAdded,
}

public sealed class AnimalWelfareCaseNote
{
    private AnimalWelfareCaseNote() { }
    public Guid Id { get; private set; }
    public Guid CaseId { get; private set; }
    public Guid AuthorUserId { get; private set; }
    public string Body { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }

    public static AnimalWelfareCaseNote Create(Guid caseId, Guid authorUserId, string body) => new()
    {
        Id = Guid.CreateVersion7(),
        CaseId = caseId,
        AuthorUserId = authorUserId,
        Body = body.Trim(),
        CreatedAt = DateTimeOffset.UtcNow,
    };
}

public sealed class AnimalWelfareReferral
{
    private AnimalWelfareReferral() { }
    public Guid Id { get; private set; }
    public Guid CaseId { get; private set; }
    public string Destination { get; private set; } = string.Empty;
    public Guid ReferredByUserId { get; private set; }
    public string Reason { get; private set; } = string.Empty;
    public DateTimeOffset ReferredAt { get; private set; }

    public static AnimalWelfareReferral Create(Guid caseId, string destination, Guid referredByUserId, string reason) => new()
    {
        Id = Guid.CreateVersion7(),
        CaseId = caseId,
        Destination = destination.Trim(),
        ReferredByUserId = referredByUserId,
        Reason = reason.Trim(),
        ReferredAt = DateTimeOffset.UtcNow,
    };
}

public sealed class AnimalWelfareCaseAuditLog
{
    private AnimalWelfareCaseAuditLog() { }
    public Guid Id { get; private set; }
    public Guid CaseId { get; private set; }
    public WelfareAuditAction Action { get; private set; }
    public Guid? ActorUserId { get; private set; }
    public string? Details { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public static AnimalWelfareCaseAuditLog Create(Guid caseId, WelfareAuditAction action, Guid? actorUserId, string? details = null) => new()
    {
        Id = Guid.CreateVersion7(),
        CaseId = caseId,
        Action = action,
        ActorUserId = actorUserId,
        Details = string.IsNullOrWhiteSpace(details) ? null : details.Trim(),
        CreatedAt = DateTimeOffset.UtcNow,
    };
}
