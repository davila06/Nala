using PawTrack.Domain.Common;

namespace PawTrack.Domain.Certificates;

public enum ClinicVeterinarianStatus
{
    PendingReview,
    Authorized,
    Rejected,
    Suspended,
    Revoked,
    Expired,
}

public sealed class ClinicVeterinarian
{
    private ClinicVeterinarian() { } // EF Core

    public Guid Id { get; private set; }
    public Guid ClinicId { get; private set; }
    public string FullName { get; private set; } = string.Empty;
    public string LicenseNumber { get; private set; } = string.Empty;
    public ClinicVeterinarianStatus Status { get; private set; }
    public bool CanIssueCertificates => IsActive;
    public string? DocumentUrl { get; private set; }
    public string? SignatureImageUrl { get; private set; }
    public Guid SubmittedByUserId { get; private set; }
    public Guid? ReviewedByAdminUserId { get; private set; }
    public DateTimeOffset? ReviewedAt { get; private set; }
    public DateOnly? ExpiresAt { get; private set; }
    public string? ReviewNotes { get; private set; }
    public string? RejectionReason { get; private set; }
    public string? SuspensionReason { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }
    public Guid? RevokedByUserId { get; private set; }
    public string? RevocationReason { get; private set; }

    public bool IsActive =>
        Status == ClinicVeterinarianStatus.Authorized &&
        RevokedAt is null &&
        (!ExpiresAt.HasValue || ExpiresAt.Value >= DateOnly.FromDateTime(DateTime.UtcNow));

    public static ClinicVeterinarian Create(Guid clinicId, string fullName, string licenseNumber)
    {
        var veterinarian = Submit(clinicId, Guid.Empty, fullName, licenseNumber);
        veterinarian.DocumentUrl = "legacy://sprint1-authorized";
        veterinarian.Authorize(Guid.Empty, DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1)), null);
        return veterinarian;
    }

    public static ClinicVeterinarian Submit(Guid clinicId, Guid submittedByUserId, string fullName, string licenseNumber)
    {
        if (clinicId == Guid.Empty) throw new ArgumentException("ClinicId is required.", nameof(clinicId));
        if (string.IsNullOrWhiteSpace(fullName)) throw new ArgumentException("Full name is required.", nameof(fullName));
        if (string.IsNullOrWhiteSpace(licenseNumber)) throw new ArgumentException("License number is required.", nameof(licenseNumber));

        return new ClinicVeterinarian
        {
            Id = Guid.CreateVersion7(),
            ClinicId = clinicId,
            FullName = fullName.Trim(),
            LicenseNumber = licenseNumber.Trim().ToUpperInvariant(),
            Status = ClinicVeterinarianStatus.PendingReview,
            SubmittedByUserId = submittedByUserId,
            CreatedAt = DateTimeOffset.UtcNow,
        };
    }

    public void AttachDocument(string documentUrl)
    {
        if (string.IsNullOrWhiteSpace(documentUrl))
            throw new ArgumentException("Document URL is required.", nameof(documentUrl));

        DocumentUrl = documentUrl.Trim();
    }

    public void AttachSignature(string signatureImageUrl)
    {
        if (string.IsNullOrWhiteSpace(signatureImageUrl))
            throw new ArgumentException("Signature URL is required.", nameof(signatureImageUrl));

        SignatureImageUrl = signatureImageUrl.Trim();
    }

    public Result<bool> Authorize(Guid reviewedByAdminUserId, DateOnly? expiresAt, string? reviewNotes)
    {
        if (string.IsNullOrWhiteSpace(DocumentUrl))
            return Result.Failure<bool>("El documento del veterinario es requerido.");

        if (!expiresAt.HasValue)
            return Result.Failure<bool>("La fecha de vencimiento del veterinario es requerida.");

        Status = ClinicVeterinarianStatus.Authorized;
        ReviewedByAdminUserId = reviewedByAdminUserId;
        ReviewedAt = DateTimeOffset.UtcNow;
        ExpiresAt = expiresAt;
        ReviewNotes = string.IsNullOrWhiteSpace(reviewNotes) ? null : reviewNotes.Trim();
        RejectionReason = null;
        SuspensionReason = null;

        return Result.Success(true);
    }

    public Result<bool> Reject(Guid reviewedByAdminUserId, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            return Result.Failure<bool>("El motivo de rechazo es requerido.");

        Status = ClinicVeterinarianStatus.Rejected;
        ReviewedByAdminUserId = reviewedByAdminUserId;
        ReviewedAt = DateTimeOffset.UtcNow;
        RejectionReason = reason.Trim();
        ReviewNotes = null;

        return Result.Success(true);
    }

    public Result<bool> Suspend(Guid reviewedByAdminUserId, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            return Result.Failure<bool>("El motivo de suspensión es requerido.");

        Status = ClinicVeterinarianStatus.Suspended;
        ReviewedByAdminUserId = reviewedByAdminUserId;
        ReviewedAt = DateTimeOffset.UtcNow;
        SuspensionReason = reason.Trim();

        return Result.Success(true);
    }

    public Result<bool> Reinstate(Guid reviewedByAdminUserId, string? notes)
    {
        if (Status != ClinicVeterinarianStatus.Suspended)
            return Result.Failure<bool>("Solo veterinarios suspendidos pueden reactivarse.");

        if (ExpiresAt.HasValue && ExpiresAt.Value < DateOnly.FromDateTime(DateTime.UtcNow))
            return Result.Failure<bool>("No se puede reactivar un veterinario vencido.");

        Status = ClinicVeterinarianStatus.Authorized;
        ReviewedByAdminUserId = reviewedByAdminUserId;
        ReviewedAt = DateTimeOffset.UtcNow;
        ReviewNotes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        SuspensionReason = null;

        return Result.Success(true);
    }

    public Result<bool> Revoke(Guid revokedByUserId, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            return Result.Failure<bool>("El motivo de revocación es requerido.");

        Status = ClinicVeterinarianStatus.Revoked;
        RevokedAt = DateTimeOffset.UtcNow;
        RevokedByUserId = revokedByUserId;
        RevocationReason = reason.Trim();

        return Result.Success(true);
    }

    public void MarkExpired()
    {
        Status = ClinicVeterinarianStatus.Expired;
    }
}
