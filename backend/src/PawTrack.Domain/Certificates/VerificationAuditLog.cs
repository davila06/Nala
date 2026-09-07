namespace PawTrack.Domain.Certificates;

public enum VerificationAuditAction
{
    ClinicVerificationSubmitted,
    ClinicVerificationDocumentUploaded,
    ClinicVerificationApproved,
    ClinicVerificationRejected,
    ClinicVerificationExpired,
    VeterinarianSubmitted,
    VeterinarianDocumentUploaded,
    VeterinarianSignatureUploaded,
    VeterinarianAuthorized,
    VeterinarianRejected,
    VeterinarianSuspended,
    VeterinarianReinstated,
    VeterinarianRevoked,
    VeterinarianExpired,
    DocumentDownloaded,
}

public sealed class VerificationAuditLog
{
    private VerificationAuditLog() { } // EF Core

    public Guid Id { get; private set; }
    public string EntityType { get; private set; } = string.Empty;
    public Guid EntityId { get; private set; }
    public VerificationAuditAction Action { get; private set; }
    public Guid? ActorUserId { get; private set; }
    public string? Details { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public static VerificationAuditLog Create(
        string entityType,
        Guid entityId,
        VerificationAuditAction action,
        Guid? actorUserId = null,
        string? details = null)
    {
        if (string.IsNullOrWhiteSpace(entityType)) throw new ArgumentException("Entity type is required.", nameof(entityType));
        if (entityId == Guid.Empty) throw new ArgumentException("EntityId is required.", nameof(entityId));

        return new VerificationAuditLog
        {
            Id = Guid.CreateVersion7(),
            EntityType = entityType.Trim(),
            EntityId = entityId,
            Action = action,
            ActorUserId = actorUserId,
            Details = string.IsNullOrWhiteSpace(details) ? null : details.Trim(),
            CreatedAt = DateTimeOffset.UtcNow,
        };
    }
}
