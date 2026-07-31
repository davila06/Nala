namespace PawTrack.Domain.Certificates;

public sealed class VetCertificate
{
    private VetCertificate() { } // EF Core

    public Guid Id { get; private set; }
    public Guid PetId { get; private set; }
    public Guid ClinicId { get; private set; }
    public Guid IssuedByUserId { get; private set; }
    public CertificateType Type { get; private set; }
    /// <summary>Free-text notes from the veterinarian (max 500 chars).</summary>
    public string? Notes { get; private set; }
    /// <summary>8-char alphanumeric code used to verify authenticity via the public endpoint.</summary>
    public string VerificationCode { get; private set; } = string.Empty;
    /// <summary>Blob Storage URL to the generated PDF/A file.</summary>
    public string? PdfUrl { get; private set; }
    public DateTimeOffset IssuedAt { get; private set; }
    /// <summary>Optional expiry (e.g. annual vaccination); null = no expiry.</summary>
    public DateTimeOffset? ValidUntil { get; private set; }
    public bool IsRevoked { get; private set; }

    // ── Factory ─────────────────────────────────────────────────────────────

    public static VetCertificate Issue(
        Guid petId,
        Guid clinicId,
        Guid issuedByUserId,
        CertificateType type,
        string verificationCode,
        string? notes = null,
        DateTimeOffset? validUntil = null)
    {
        return new VetCertificate
        {
            Id = Guid.CreateVersion7(),
            PetId = petId,
            ClinicId = clinicId,
            IssuedByUserId = issuedByUserId,
            Type = type,
            VerificationCode = verificationCode,
            Notes = notes,
            ValidUntil = validUntil,
            IssuedAt = DateTimeOffset.UtcNow,
        };
    }

    // ── Behaviour ───────────────────────────────────────────────────────────

    public void SetPdfUrl(string url) => PdfUrl = url;

    public void Revoke() => IsRevoked = true;

    public bool IsValid => !IsRevoked && (ValidUntil is null || ValidUntil > DateTimeOffset.UtcNow);
}
