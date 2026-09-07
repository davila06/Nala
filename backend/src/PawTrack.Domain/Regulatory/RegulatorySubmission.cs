using PawTrack.Domain.Common;

namespace PawTrack.Domain.Regulatory;

public enum RegulatorySubmissionStatus
{
    Prepared,
    Queued,
    Submitted,
    Acknowledged,
    Rejected,
    Failed,
    Cancelled,
}

public sealed class RegulatorySubmission
{
    private RegulatorySubmission() { } // EF Core

    public Guid Id { get; private set; }
    public Guid ExportId { get; private set; }
    public string Destination { get; private set; } = string.Empty;
    public string SubmissionType { get; private set; } = string.Empty;
    public string IdempotencyKey { get; private set; } = string.Empty;
    public string PayloadSha256 { get; private set; } = string.Empty;
    public string? PayloadBlobUrl { get; private set; }
    public RegulatorySubmissionStatus Status { get; private set; }
    public DateTimeOffset? SubmittedAt { get; private set; }
    public DateTimeOffset? AcknowledgedAt { get; private set; }
    public string? ExternalReference { get; private set; }
    public string? ErrorCode { get; private set; }
    public int RetryCount { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public static RegulatorySubmission Prepare(
        Guid exportId,
        string destination,
        string submissionType,
        string idempotencyKey,
        string payloadSha256,
        string? payloadBlobUrl = null)
    {
        if (exportId == Guid.Empty) throw new ArgumentException("Export is required.", nameof(exportId));
        if (string.IsNullOrWhiteSpace(destination)) throw new ArgumentException("Destination is required.", nameof(destination));
        if (string.IsNullOrWhiteSpace(submissionType)) throw new ArgumentException("Submission type is required.", nameof(submissionType));
        if (string.IsNullOrWhiteSpace(idempotencyKey)) throw new ArgumentException("Idempotency key is required.", nameof(idempotencyKey));
        if (payloadSha256.Length != 64 || !payloadSha256.All(Uri.IsHexDigit))
            throw new ArgumentException("Payload hash must be SHA-256.", nameof(payloadSha256));

        return new RegulatorySubmission
        {
            Id = Guid.CreateVersion7(),
            ExportId = exportId,
            Destination = destination.Trim(),
            SubmissionType = submissionType.Trim(),
            IdempotencyKey = idempotencyKey.Trim(),
            PayloadSha256 = payloadSha256.Trim().ToLowerInvariant(),
            PayloadBlobUrl = string.IsNullOrWhiteSpace(payloadBlobUrl) ? null : payloadBlobUrl.Trim(),
            Status = RegulatorySubmissionStatus.Prepared,
            CreatedAt = DateTimeOffset.UtcNow,
        };
    }

    public Result<bool> Queue()
    {
        if (Status != RegulatorySubmissionStatus.Prepared)
            return Result.Failure<bool>("La submission no está preparada para encolarse.");
        Status = RegulatorySubmissionStatus.Queued;
        return Result.Success(true);
    }

    public void AttachPayloadBlob(string blobUrl)
    {
        if (!string.IsNullOrWhiteSpace(blobUrl)) PayloadBlobUrl = blobUrl.Trim();
    }

    public Result<bool> MarkSubmitted(DateTimeOffset submittedAt, string? externalReference = null)
    {
        if (Status != RegulatorySubmissionStatus.Queued)
            return Result.Failure<bool>("La submission no está encolada.");
        Status = RegulatorySubmissionStatus.Submitted;
        SubmittedAt = submittedAt;
        ExternalReference = string.IsNullOrWhiteSpace(externalReference) ? null : externalReference.Trim();
        return Result.Success(true);
    }

    public Result<bool> Acknowledge(DateTimeOffset acknowledgedAt, string? externalReference = null)
    {
        if (Status != RegulatorySubmissionStatus.Submitted)
            return Result.Failure<bool>("La submission no ha sido enviada.");
        Status = RegulatorySubmissionStatus.Acknowledged;
        AcknowledgedAt = acknowledgedAt;
        ExternalReference = string.IsNullOrWhiteSpace(externalReference) ? ExternalReference : externalReference.Trim();
        return Result.Success(true);
    }

    public Result<bool> Fail(string errorCode)
    {
        if (Status is RegulatorySubmissionStatus.Acknowledged or RegulatorySubmissionStatus.Cancelled)
            return Result.Failure<bool>("La submission ya no puede fallar.");
        if (string.IsNullOrWhiteSpace(errorCode)) return Result.Failure<bool>("El código de error es requerido.");
        Status = RegulatorySubmissionStatus.Failed;
        ErrorCode = errorCode.Trim();
        RetryCount++;
        return Result.Success(true);
    }

    public Result<bool> Retry()
    {
        if (Status != RegulatorySubmissionStatus.Failed)
            return Result.Failure<bool>("Solo una submission fallida puede reintentarse.");
        if (RetryCount >= 3)
            return Result.Failure<bool>("Se alcanzó el máximo de reintentos.");
        Status = RegulatorySubmissionStatus.Queued;
        ErrorCode = null;
        return Result.Success(true);
    }

    public Result<bool> Cancel()
    {
        if (Status is RegulatorySubmissionStatus.Submitted or RegulatorySubmissionStatus.Acknowledged)
            return Result.Failure<bool>("La submission ya fue enviada.");
        Status = RegulatorySubmissionStatus.Cancelled;
        return Result.Success(true);
    }
}
