namespace PawTrack.Domain.Imports;

public enum ImportJobStatus
{
    Pending = 1,
    Processing = 2,
    Completed = 3,
    CompletedWithErrors = 4,
    Failed = 5,
    Cancelled = 6,
}

public sealed class ImportRowError
{
    private ImportRowError() { }

    public Guid Id { get; private set; }
    public Guid ImportJobId { get; private set; }
    public int RowNumber { get; private set; }
    public string Column { get; private set; } = string.Empty;
    public string Code { get; private set; } = string.Empty;
    public string Message { get; private set; } = string.Empty;
    public string? RawValue { get; private set; }

    internal static ImportRowError Create(
        Guid importJobId, int rowNumber, string column, string code, string message, string? rawValue) => new()
        {
            Id = Guid.CreateVersion7(),
            ImportJobId = importJobId,
            RowNumber = rowNumber,
            Column = column.Trim(),
            Code = code.Trim(),
            Message = message.Trim(),
            RawValue = rawValue,
        };
}

public sealed class ImportJob
{
    private readonly List<ImportRowError> _errors = [];
    private ImportJob() { }

    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public string ResourceType { get; private set; } = string.Empty;
    public string Format { get; private set; } = string.Empty;
    public string FileHash { get; private set; } = string.Empty;
    public string IdempotencyKey { get; private set; } = string.Empty;
    public int RowCount { get; private set; }
    public int ImportedCount { get; private set; }
    public int DuplicateCount { get; private set; }
    public ImportJobStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public IReadOnlyList<ImportRowError> Errors => _errors.AsReadOnly();

    public static ImportJob Create(
        Guid tenantId, string resourceType, string format, string fileHash,
        string idempotencyKey, int rowCount)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("Tenant is required.", nameof(tenantId));
        if (string.IsNullOrWhiteSpace(resourceType)) throw new ArgumentException("Resource type is required.", nameof(resourceType));
        if (format is not ("csv" or "json")) throw new ArgumentException("Format must be csv or json.", nameof(format));
        if (string.IsNullOrWhiteSpace(fileHash)) throw new ArgumentException("File hash is required.", nameof(fileHash));
        if (string.IsNullOrWhiteSpace(idempotencyKey)) throw new ArgumentException("Idempotency key is required.", nameof(idempotencyKey));
        if (rowCount < 0) throw new ArgumentOutOfRangeException(nameof(rowCount));

        return new ImportJob
        {
            Id = Guid.CreateVersion7(),
            TenantId = tenantId,
            ResourceType = resourceType.Trim(),
            Format = format,
            FileHash = fileHash.Trim(),
            IdempotencyKey = idempotencyKey.Trim(),
            RowCount = rowCount,
            Status = ImportJobStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow,
        };
    }

    public void Start() => Status = ImportJobStatus.Processing;
    public void AddImported() => ImportedCount++;
    public void AddDuplicate() => DuplicateCount++;
    public void AddError(int rowNumber, string column, string code, string message, string? rawValue) =>
        _errors.Add(ImportRowError.Create(Id, rowNumber, column, code, message, rawValue));

    public void Complete()
    {
        Status = _errors.Count == 0 ? ImportJobStatus.Completed : ImportJobStatus.CompletedWithErrors;
        CompletedAt = DateTimeOffset.UtcNow;
    }

    public void Fail() { Status = ImportJobStatus.Failed; CompletedAt = DateTimeOffset.UtcNow; }
}
