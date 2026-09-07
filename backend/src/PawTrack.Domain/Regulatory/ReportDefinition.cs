namespace PawTrack.Domain.Regulatory;

public sealed class ReportDefinition
{
    private ReportDefinition() { } // EF Core

    public Guid Id { get; private set; }
    public ReportType ReportType { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string SchemaVersion { get; private set; } = string.Empty;
    public ExportScope Scope { get; private set; }
    public int SuppressionThreshold { get; private set; }
    public int RetentionDays { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public static ReportDefinition Create(
        ReportType reportType,
        string code,
        string name,
        string schemaVersion,
        ExportScope scope,
        int suppressionThreshold,
        int retentionDays)
    {
        if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("Report code is required.", nameof(code));
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Report name is required.", nameof(name));
        if (string.IsNullOrWhiteSpace(schemaVersion)) throw new ArgumentException("Schema version is required.", nameof(schemaVersion));
        ArgumentOutOfRangeException.ThrowIfLessThan(suppressionThreshold, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(retentionDays, 1);

        var now = DateTimeOffset.UtcNow;
        return new ReportDefinition
        {
            Id = Guid.CreateVersion7(),
            ReportType = reportType,
            Code = code.Trim().ToUpperInvariant(),
            Name = name.Trim(),
            SchemaVersion = schemaVersion.Trim(),
            Scope = scope,
            SuppressionThreshold = suppressionThreshold,
            RetentionDays = retentionDays,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now,
        };
    }

    public void Deactivate() { IsActive = false; UpdatedAt = DateTimeOffset.UtcNow; }
    public void Activate() { IsActive = true; UpdatedAt = DateTimeOffset.UtcNow; }
}
