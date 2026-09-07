using PawTrack.Domain.Common;

namespace PawTrack.Domain.ServiceProviders;

public sealed class ProviderVerification
{
    private ProviderVerification() { }

    public Guid Id { get; private set; }
    public Guid ServiceProviderId { get; private set; }
    public Guid SubmittedByUserId { get; private set; }
    public string? DocumentUrl { get; private set; }
    public string? DocumentContentType { get; private set; }
    public ProviderVerificationStatus Status { get; private set; }
    public DateTimeOffset SubmittedAt { get; private set; }
    public Guid? ReviewedByAdminUserId { get; private set; }
    public DateTimeOffset? ReviewedAt { get; private set; }
    public DateOnly? ExpiresAt { get; private set; }
    public string? ReviewNotes { get; private set; }
    public string? RejectionReason { get; private set; }
    public DateTimeOffset? SupersededAt { get; private set; }

    public bool IsActive => Status == ProviderVerificationStatus.Verified &&
        SupersededAt is null &&
        ExpiresAt.HasValue && ExpiresAt.Value >= DateOnly.FromDateTime(DateTime.UtcNow);

    public static ProviderVerification Submit(Guid serviceProviderId, Guid submittedByUserId)
    {
        if (serviceProviderId == Guid.Empty) throw new ArgumentException("Service provider ID is required.", nameof(serviceProviderId));
        if (submittedByUserId == Guid.Empty) throw new ArgumentException("Submitting user ID is required.", nameof(submittedByUserId));

        return new ProviderVerification
        {
            Id = Guid.CreateVersion7(),
            ServiceProviderId = serviceProviderId,
            SubmittedByUserId = submittedByUserId,
            Status = ProviderVerificationStatus.Pending,
            SubmittedAt = DateTimeOffset.UtcNow,
        };
    }

    public void AttachDocument(string documentUrl, string contentType = "application/octet-stream")
    {
        if (string.IsNullOrWhiteSpace(documentUrl)) throw new ArgumentException("Document URL is required.", nameof(documentUrl));
        DocumentUrl = documentUrl.Trim();
        DocumentContentType = string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType.Trim();
    }

    public Result<bool> Verify(Guid adminUserId, DateOnly? expiresAt, string? notes)
    {
        if (string.IsNullOrWhiteSpace(DocumentUrl)) return Result.Failure<bool>("El documento de verificacion es requerido.");
        if (!expiresAt.HasValue) return Result.Failure<bool>("La fecha de vencimiento es requerida.");

        ReviewedByAdminUserId = adminUserId;
        ReviewedAt = DateTimeOffset.UtcNow;
        ExpiresAt = expiresAt;
        ReviewNotes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        RejectionReason = null;
        Status = ProviderVerificationStatus.Verified;
        return Result.Success(true);
    }

    public Result<bool> Reject(Guid adminUserId, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason)) return Result.Failure<bool>("El motivo de rechazo es requerido.");

        ReviewedByAdminUserId = adminUserId;
        ReviewedAt = DateTimeOffset.UtcNow;
        ExpiresAt = null;
        RejectionReason = reason.Trim();
        ReviewNotes = null;
        Status = ProviderVerificationStatus.Rejected;
        return Result.Success(true);
    }

    public void MarkExpired()
    {
        if (Status == ProviderVerificationStatus.Verified)
            Status = ProviderVerificationStatus.Expired;
    }

    public void Supersede()
    {
        SupersededAt = DateTimeOffset.UtcNow;
    }
}