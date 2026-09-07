using PawTrack.Application.Regulatory.Dtos;
using PawTrack.Application.Regulatory.Interfaces;
using PawTrack.Application.Regulatory.Renderers;
using PawTrack.Domain.Common;
using PawTrack.Domain.Regulatory;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PawTrack.Infrastructure.Regulatory;

public sealed class QuestPdfRegulatoryExportRenderer : IRegulatoryExportRenderer
{
    static QuestPdfRegulatoryExportRenderer()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public Result<RegulatoryExportPayload> Render(ExportFormat format, RegulatoryReportData reportData)
    {
        if (format != ExportFormat.Pdf)
            return new RegulatoryExportRenderer().Render(format, reportData);

        var bytes = Document.Create(document =>
        {
            document.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(style => style.FontSize(10).FontFamily(Fonts.Arial));
                page.Header().Column(column =>
                {
                    column.Item().Text("PawTrack CR / NALA").Bold().FontSize(20).FontColor(Colors.Orange.Medium);
                    column.Item().Text("Reporte institucional SENASA-ready").FontSize(10).FontColor(Colors.Grey.Darken1);
                    column.Item().PaddingTop(6).LineHorizontal(1.2f).LineColor(Colors.Orange.Medium);
                });
                page.Content().PaddingVertical(20).Column(column =>
                {
                    column.Item().Text($"Reporte {reportData.ReportType}").Bold().FontSize(16).FontColor(Colors.Grey.Darken3);
                    column.Item().PaddingTop(4).Text($"Periodo: {reportData.PeriodStart:dd/MM/yyyy} - {reportData.PeriodEnd:dd/MM/yyyy}");
                    column.Item().PaddingTop(12).Background(Colors.Grey.Lighten4).Padding(12).Column(section =>
                    {
                        foreach (var row in reportData.Rows)
                            AddMetric(section, $"{row.Dimension} / {row.Category}", row.IsSuppressed ? "SUPPRESSED" : row.Value.ToString());
                    });
                    if (reportData.IsSuppressed)
                        column.Item().PaddingTop(8).Text("Algunos valores fueron suprimidos por privacidad.").Italic().FontSize(9);
                    column.Item().PaddingTop(18).Text(
                        "Este reporte es compatible con procesos institucionales y no constituye un documento oficial, aprobación o envío a SENASA.")
                        .FontSize(9).FontColor(Colors.Grey.Darken1);
                });
                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span($"Generado: {DateTimeOffset.UtcNow:dd/MM/yyyy HH:mm} UTC | ");
                    text.Span("PawTrack CR / NALA");
                });
            });
        }).GeneratePdf();

        return Result.Success(new RegulatoryExportPayload(bytes, "application/pdf", "pdf"));
    }

    public Result<RegulatoryExportPayload> Render(ExportFormat format, NalaOverviewDto overview) =>
        Render(format, new RegulatoryReportData(
            ReportType.NalaOverview,
            overview.PeriodStart,
            overview.PeriodEnd,
            OverviewRows(overview),
            overview.IsSuppressed,
            overview));

    private static IReadOnlyList<RegulatoryReportRow> OverviewRows(NalaOverviewDto overview) =>
    [
        new("ActiveLostPets", "Nacional", overview.ActiveLostPets, false),
        new("ReunitedPets", "Nacional", overview.ReunitedPets, false),
        new("CapturedAnimals", "Nacional", overview.CapturedAnimals, false),
        new("AvailableAdoptions", "Nacional", overview.AvailableAdoptions, false),
        new("AdoptedAnimals", "Nacional", overview.AdoptedAnimals, false),
        new("OpenWelfareCases", "Nacional", overview.OpenWelfareCases, false),
        new("VerifiedMicrochips", "Nacional", overview.VerifiedMicrochips, false),
        new("ValidCertificates", "Nacional", overview.ValidCertificates, false),
    ];

    private static void AddMetric(ColumnDescriptor column, string label, string value)
    {
        column.Item().PaddingVertical(3).Row(row =>
        {
            row.RelativeItem().Text(label);
            row.ConstantItem(100).AlignRight().Text(value).Bold();
        });
    }
}
