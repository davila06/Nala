using PawTrack.Domain.Common;

namespace PawTrack.Domain.Regulatory;

public enum ReportType
{
    Recovery,
    MunicipalCaptures,
    Adoptions,
    WelfareCases,
    SanitaryIdentity,
    NetworkCoverage,
    NalaOverview,
}

public enum ExportScope
{
    Public,
    Institutional,
    Admin,
    Nala,
}

public enum ExportFormat
{
    Csv,
    Json,
    Pdf,
}

public enum RegulatoryExportStatus
{
    Requested,
    Running,
    Completed,
    Failed,
    Expired,
}

public sealed class RegulatoryExport
{
    private RegulatoryExport() { } // EF Core

    public Guid Id { get; private set; }
    public string ExportCode { get; private set; } = string.Empty;
    public ReportType ReportType { get; private set; }
    public ExportScope Scope { get; private set; }
    public ExportFormat Format { get; private set; }
    public string SchemaVersion { get; private set; } = string.Empty;
    public Guid RequestedByUserId { get; private set; }
    public Guid? OrganizationId { get; private set; }
    public string? Canton { get; private set; }
    public DateOnly PeriodStart { get; private set; }
    public DateOnly PeriodEnd { get; private set; }
    public string TimeZone { get; private set; } = string.Empty;
    public string IdempotencyKey { get; private set; } = string.Empty;
    public RegulatoryExportStatus Status { get; private set; }
    public int? RowCount { get; private set; }
    public int? SuppressedRowCount { get; private set; }
    public string? PayloadSha256 { get; private set; }
    public string? BlobUrl { get; private set; }
    public string? ErrorCode { get; private set; }
    public DateTimeOffset RequestedAt { get; private set; }
    public DateTimeOffset? StartedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public DateTimeOffset? ExpiresAt { get; private set; }
    public DateTimeOffset? DownloadedAt { get; private set; }

    public bool IsDownloadable => Status == RegulatoryExportStatus.Completed
        && !string.IsNullOrWhiteSpace(BlobUrl)
        && ExpiresAt is not null && ExpiresAt > DateTimeOffset.UtcNow;

    public static RegulatoryExport Request(
        ReportType reportType,
        ExportScope scope,
        ExportFormat format,
        string schemaVersion,
        Guid requestedByUserId,
        Guid? organizationId,
        string? canton,
        DateOnly periodStart,
        DateOnly periodEnd,
        string timeZone,
        string idempotencyKey)
    {
        if (periodEnd < periodStart) throw new ArgumentException("Period end must not precede period start.", nameof(periodEnd));
        if (requestedByUserId == Guid.Empty) throw new ArgumentException("Requesting user is required.", nameof(requestedByUserId));
        if (string.IsNullOrWhiteSpace(schemaVersion)) throw new ArgumentException("Schema version is required.", nameof(schemaVersion));
        if (string.IsNullOrWhiteSpace(timeZone)) throw new ArgumentException("Time zone is required.", nameof(timeZone));
        if (string.IsNullOrWhiteSpace(idempotencyKey)) throw new ArgumentException("Idempotency key is required.", nameof(idempotencyKey));

        var now = DateTimeOffset.UtcNow;
        return new RegulatoryExport
        {
            Id = Guid.CreateVersion7(),
            ExportCode = CreateExportCode(),
            ReportType = reportType,
            Scope = scope,
            Format = format,
            SchemaVersion = schemaVersion.Trim(),
            RequestedByUserId = requestedByUserId,
            OrganizationId = organizationId,
            Canton = string.IsNullOrWhiteSpace(canton) ? null : canton.Trim(),
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            TimeZone = timeZone.Trim(),
            IdempotencyKey = idempotencyKey.Trim(),
            Status = RegulatoryExportStatus.Requested,
            RequestedAt = now,
        };
    }

    public Result<bool> Start(DateTimeOffset startedAt)
    {
        if (Status != RegulatoryExportStatus.Requested)
            return Result.Failure<bool>("El export no puede iniciar desde su estado actual.");
        Status = RegulatoryExportStatus.Running;
        StartedAt = startedAt;
        return Result.Success(true);
    }

    public Result<bool> Complete(
        int rowCount,
        int suppressedRowCount,
        string payloadSha256,
        string blobUrl,
        DateTimeOffset completedAt,
        DateTimeOffset expiresAt)
    {
        if (Status != RegulatoryExportStatus.Running)
            return Result.Failure<bool>("El export no está en ejecución.");
        if (rowCount < 0 || suppressedRowCount < 0 || suppressedRowCount > rowCount)
            return Result.Failure<bool>("Los conteos del export no son válidos.");
        if (!IsSha256(payloadSha256)) return Result.Failure<bool>("El hash del payload no es válido.");
        if (string.IsNullOrWhiteSpace(blobUrl)) return Result.Failure<bool>("El Blob del export es requerido.");
        if (expiresAt <= completedAt) return Result.Failure<bool>("La expiración debe ser posterior a la finalización.");

        RowCount = rowCount;
        SuppressedRowCount = suppressedRowCount;
        PayloadSha256 = payloadSha256.Trim().ToLowerInvariant();
        BlobUrl = blobUrl.Trim();
        CompletedAt = completedAt;
        ExpiresAt = expiresAt;
        Status = RegulatoryExportStatus.Completed;
        return Result.Success(true);
    }

    public Result<bool> Fail(string errorCode)
    {
        if (Status is RegulatoryExportStatus.Completed or RegulatoryExportStatus.Expired)
            return Result.Failure<bool>("El export ya no puede marcarse como fallido.");
        if (string.IsNullOrWhiteSpace(errorCode)) return Result.Failure<bool>("El código de error es requerido.");
        ErrorCode = errorCode.Trim();
        Status = RegulatoryExportStatus.Failed;
        return Result.Success(true);
    }

    public Result<bool> Expire(DateTimeOffset expiredAt)
    {
        if (Status != RegulatoryExportStatus.Completed)
            return Result.Failure<bool>("Solo se pueden expirar exports completados.");
        if (ExpiresAt is null || expiredAt < ExpiresAt)
            return Result.Failure<bool>("El export todavía no ha alcanzado su fecha de expiración.");
        Status = RegulatoryExportStatus.Expired;
        return Result.Success(true);
    }

    public Result<bool> RecordDownload(DateTimeOffset downloadedAt)
    {
        if (!IsDownloadable) return Result.Failure<bool>("El export no está disponible para descarga.");
        DownloadedAt = downloadedAt;
        return Result.Success(true);
    }

    private static bool IsSha256(string value) =>
        value.Length == 64 && value.All(Uri.IsHexDigit);

    private static string CreateExportCode()
    {
        Span<byte> bytes = stackalloc byte[6];
        System.Security.Cryptography.RandomNumberGenerator.Fill(bytes);
        return $"EXP-{Convert.ToHexString(bytes)}";
    }
}
