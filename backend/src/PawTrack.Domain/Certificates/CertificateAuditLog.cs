namespace PawTrack.Domain.Certificates;

public enum CertificateAuditAction
{
    Issued,
    PdfGenerated,
    VerifiedPublicly,
    Downloaded,
    Revoked,
}

public sealed class CertificateAuditLog
{
    private CertificateAuditLog() { } // EF Core

    public Guid Id { get; private set; }
    public Guid CertificateId { get; private set; }
    public CertificateAuditAction Action { get; private set; }
    public Guid? ActorUserId { get; private set; }
    public string? Details { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public static CertificateAuditLog Create(
        Guid certificateId,
        CertificateAuditAction action,
        Guid? actorUserId = null,
        string? details = null)
    {
        if (certificateId == Guid.Empty) throw new ArgumentException("CertificateId is required.", nameof(certificateId));

        return new CertificateAuditLog
        {
            Id = Guid.CreateVersion7(),
            CertificateId = certificateId,
            Action = action,
            ActorUserId = actorUserId,
            Details = string.IsNullOrWhiteSpace(details) ? null : details.Trim(),
            CreatedAt = DateTimeOffset.UtcNow,
        };
    }
}
