using PawTrack.Domain.Regulatory;

namespace PawTrack.Application.Regulatory.Dtos;

public sealed record ReportDefinitionDto(
    string Code,
    ReportType ReportType,
    string Name,
    string SchemaVersion,
    ExportScope Scope,
    int SuppressionThreshold,
    int RetentionDays);

public sealed record RegulatoryExportDto(
    Guid Id,
    string ExportCode,
    ReportType ReportType,
    ExportScope Scope,
    ExportFormat Format,
    string SchemaVersion,
    RegulatoryExportStatus Status,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    int? RowCount,
    int? SuppressedRowCount,
    string? PayloadSha256,
    DateTimeOffset RequestedAt,
    DateTimeOffset? CompletedAt,
    DateTimeOffset? ExpiresAt);

public sealed record PagedRegulatoryExportsDto(
    IReadOnlyList<RegulatoryExportDto> Items,
    int Page,
    int PageSize);

public sealed record RegulatoryReportRow(
    string Category,
    string Dimension,
    int Value,
    bool IsSuppressed);

public sealed record RegulatoryReportData(
    ReportType ReportType,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    IReadOnlyList<RegulatoryReportRow> Rows,
    bool IsSuppressed,
    NalaOverviewDto? Overview);

public sealed record RegulatoryReportFilter(
    string? Canton,
    string? Species,
    string? Status,
    Guid? OrganizationId);

public sealed record RegulatoryReportPreviewDto(
    ReportType ReportType,
    string SchemaVersion,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    IReadOnlyList<RegulatoryReportRow> Rows,
    int SuppressedRowCount,
    bool IsSuppressed);
