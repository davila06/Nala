using PawTrack.Application.Regulatory.Dtos;
using PawTrack.Domain.Regulatory;

namespace PawTrack.Application.Regulatory;

internal static class RegulatoryMappers
{
    public static ReportDefinitionDto ToDto(this ReportDefinition definition) => new(
        definition.Code,
        definition.ReportType,
        definition.Name,
        definition.SchemaVersion,
        definition.Scope,
        definition.SuppressionThreshold,
        definition.RetentionDays);

    public static RegulatoryExportDto ToDto(this RegulatoryExport export) => new(
        export.Id,
        export.ExportCode,
        export.ReportType,
        export.Scope,
        export.Format,
        export.SchemaVersion,
        export.Status,
        export.PeriodStart,
        export.PeriodEnd,
        export.RowCount,
        export.SuppressedRowCount,
        export.PayloadSha256,
        export.RequestedAt,
        export.CompletedAt,
        export.ExpiresAt);
}
