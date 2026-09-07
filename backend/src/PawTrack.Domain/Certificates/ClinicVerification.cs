using PawTrack.Domain.Common;

namespace PawTrack.Domain.Certificates;

public enum ClinicVerificationStatus
{
    Pending,
    Verified,
    Rejected,
    Expired,
}

public sealed class ClinicVerification
{
    private ClinicVerification() { } // EF Core

    public Guid Id { get; private set; }
    public Guid ClinicId { get; private set; }
    public string LicenseNumberSnapshot { get; private set; } = string.Empty;
    public string? DocumentUrl { get; private set; }
    public Guid SubmittedByUserId { get; private set; }
    public ClinicVerificationStatus Status { get; private set; }
    public DateTimeOffset SubmittedAt { get; private set; }
    public DateTimeOffset? VerifiedAt { get; private set; }
    public Guid? VerifiedByAdminUserId { get; private set; }
    public Guid? ReviewedByAdminUserId { get; private set; }
    public DateTimeOffset? ReviewedAt { get; private set; }
    public string? ReviewNotes { get; private set; }
    public DateOnly? ExpiresAt { get; private set; }
    public string? RejectionReason { get; private set; }
    public DateTimeOffset? RevalidationRequestedAt { get; private set; }
    public DateTimeOffset? SupersededAt { get; private set; }

    public bool IsActive =>
        Status == ClinicVerificationStatus.Verified &&
        SupersededAt is null &&
        (!ExpiresAt.HasValue || ExpiresAt.Value >= DateOnly.FromDateTime(DateTime.UtcNow));

    public static ClinicVerification Submit(Guid clinicId, string licenseNumber) =>
        Submit(clinicId, licenseNumber, Guid.Empty);

    public static ClinicVerification Submit(Guid clinicId, string licenseNumber, Guid submittedByUserId)
    {
        if (clinicId == Guid.Empty) throw new ArgumentException("ClinicId is required.", nameof(clinicId));
        if (string.IsNullOrWhiteSpace(licenseNumber)) throw new ArgumentException("License number is required.", nameof(licenseNumber));

        return new ClinicVerification
        {
            Id = Guid.CreateVersion7(),
            ClinicId = clinicId,
            LicenseNumberSnapshot = licenseNumber.Trim().ToUpperInvariant(),
            SubmittedByUserId = submittedByUserId,
            Status = ClinicVerificationStatus.Pending,
            SubmittedAt = DateTimeOffset.UtcNow,
        };
    }

    public void AttachDocument(string documentUrl)
    {
        if (string.IsNullOrWhiteSpace(documentUrl))
            throw new ArgumentException("Document URL is required.", nameof(documentUrl));

        DocumentUrl = documentUrl.Trim();
    }

    public Result<bool> Verify(Guid verifiedByAdminUserId, DateOnly? expiresAt) =>
        Verify(verifiedByAdminUserId, expiresAt, null);

    public Result<bool> Verify(Guid verifiedByAdminUserId, DateOnly? expiresAt, string? reviewNotes)
    {
        if (string.IsNullOrWhiteSpace(DocumentUrl))
            return Result.Failure<bool>("El documento de verificación es requerido.");

        if (!expiresAt.HasValue)
            return Result.Failure<bool>("La fecha de vencimiento de la verificación es requerida.");

        VerifiedByAdminUserId = verifiedByAdminUserId;
        ReviewedByAdminUserId = verifiedByAdminUserId;
        VerifiedAt = DateTimeOffset.UtcNow;
        ReviewedAt = DateTimeOffset.UtcNow;
        ExpiresAt = expiresAt;
        RejectionReason = null;
        ReviewNotes = string.IsNullOrWhiteSpace(reviewNotes) ? null : reviewNotes.Trim();
        Status = ClinicVerificationStatus.Verified;

        return Result.Success(true);
    }

    public Result<bool> Reject(Guid reviewedByAdminUserId, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            return Result.Failure<bool>("El motivo de rechazo es requerido.");

        VerifiedByAdminUserId = reviewedByAdminUserId;
        ReviewedByAdminUserId = reviewedByAdminUserId;
        VerifiedAt = null;
        ReviewedAt = DateTimeOffset.UtcNow;
        ExpiresAt = null;
        RejectionReason = reason.Trim();
        ReviewNotes = null;
        Status = ClinicVerificationStatus.Rejected;

        return Result.Success(true);
    }

    public void MarkExpired()
    {
        Status = ClinicVerificationStatus.Expired;
    }

    public void RequestRevalidation()
    {
        RevalidationRequestedAt = DateTimeOffset.UtcNow;
    }

    public void Supersede()
    {
        SupersededAt = DateTimeOffset.UtcNow;
    }
}
