using System.Text;
using System.Text.Json;
using PawTrack.Application.Regulatory.Dtos;
using PawTrack.Application.Regulatory.Interfaces;
using PawTrack.Domain.Common;
using PawTrack.Domain.Regulatory;

namespace PawTrack.Application.Regulatory.Renderers;

public sealed class RegulatoryExportRenderer : IRegulatoryExportRenderer
{
    public Result<RegulatoryExportPayload> Render(ExportFormat format, RegulatoryReportData reportData)
    {
        return format switch
        {
            ExportFormat.Json => Result.Success(new RegulatoryExportPayload(
                JsonSerializer.SerializeToUtf8Bytes(reportData, new JsonSerializerOptions { WriteIndented = true }),
                "application/json",
                "json")),
            ExportFormat.Csv => Result.Success(new RegulatoryExportPayload(
                RenderCsv(reportData),
                "text/csv; charset=utf-8",
                "csv")),
            _ => Result.Failure<RegulatoryExportPayload>("El renderer base no soporta PDF."),
        };
    }

    public Result<RegulatoryExportPayload> Render(ExportFormat format, NalaOverviewDto overview) =>
        Render(format, new RegulatoryReportData(
            ReportType.NalaOverview,
            overview.PeriodStart,
            overview.PeriodEnd,
            [],
            overview.IsSuppressed,
            overview));

    private static byte[] RenderCsv(RegulatoryReportData reportData)
    {
        var rows = reportData.Rows.Select(row => new ReportRow(
            row.Dimension,
            row.Category,
            row.IsSuppressed ? "SUPPRESSED" : row.Value.ToString()));
        return CsvReportRenderer.RenderUtf8(rows);
    }
}
