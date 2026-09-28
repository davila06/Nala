namespace PawTrack.Domain.Medical;

public enum HealthReportExportStatus
{
    Queued,
    Processing,
    Completed,
    Failed,
    Expired,
}

public sealed class HealthReportExport
{
    private HealthReportExport() { }

    public Guid Id { get; private set; }
    public Guid PetId { get; private set; }
    public Guid RequestedByUserId { get; private set; }
    public HealthReportExportStatus Status { get; private set; }
    public string? BlobUrl { get; private set; }
    public string? ErrorCode { get; private set; }
    public int? ItemCount { get; private set; }
    public int AttemptCount { get; private set; }
    public DateTimeOffset RequestedAt { get; private set; }
    public DateTimeOffset? StartedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }

    public bool IsDownloadable => Status == HealthReportExportStatus.Completed &&
        !string.IsNullOrWhiteSpace(BlobUrl) && ExpiresAt > DateTimeOffset.UtcNow;

    public static HealthReportExport Queue(Guid petId, Guid requestedByUserId, TimeSpan lifetime)
    {
        if (petId == Guid.Empty) throw new ArgumentException("Pet is required.", nameof(petId));
        if (requestedByUserId == Guid.Empty) throw new ArgumentException("Requester is required.", nameof(requestedByUserId));
        if (lifetime <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(lifetime));
        var now = DateTimeOffset.UtcNow;
        return new HealthReportExport
        {
            Id = Guid.CreateVersion7(),
            PetId = petId,
            RequestedByUserId = requestedByUserId,
            Status = HealthReportExportStatus.Queued,
            RequestedAt = now,
            ExpiresAt = now.Add(lifetime),
        };
    }

    public bool Start(DateTimeOffset startedAt)
    {
        if (Status != HealthReportExportStatus.Queued) return false;
        Status = HealthReportExportStatus.Processing;
        StartedAt = startedAt;
        AttemptCount++;
        return true;
    }

    public bool RecoverInterrupted(DateTimeOffset staleBefore)
    {
        if (Status != HealthReportExportStatus.Processing || StartedAt >= staleBefore) return false;
        if (AttemptCount >= 3)
        {
            Fail("worker_interrupted");
            return true;
        }
        Status = HealthReportExportStatus.Queued;
        StartedAt = null;
        return true;
    }

    public bool Complete(string blobUrl, int itemCount, DateTimeOffset completedAt)
    {
        if (Status != HealthReportExportStatus.Processing || string.IsNullOrWhiteSpace(blobUrl) || itemCount < 0)
            return false;
        BlobUrl = blobUrl.Trim();
        ItemCount = itemCount;
        CompletedAt = completedAt;
        Status = HealthReportExportStatus.Completed;
        return true;
    }

    public bool Fail(string errorCode)
    {
        if (Status is not (HealthReportExportStatus.Queued or HealthReportExportStatus.Processing) || string.IsNullOrWhiteSpace(errorCode))
            return false;
        ErrorCode = errorCode.Trim();
        Status = HealthReportExportStatus.Failed;
        return true;
    }

    public bool Expire(DateTimeOffset now)
    {
        if (Status != HealthReportExportStatus.Completed || now < ExpiresAt) return false;
        Status = HealthReportExportStatus.Expired;
        BlobUrl = null;
        return true;
    }
}
