using PawTrack.Application.Medical;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PawTrack.Infrastructure.Medical;

public sealed class QuestPdfConsolidatedHealthReportGenerator : IConsolidatedHealthPdfGenerator
{
    public Task<byte[]> GenerateAsync(ConsolidatedHealthReportData data, CancellationToken cancellationToken)
    {
        QuestPDF.Settings.License = LicenseType.Community;
        var bytes = Document.Create(document =>
        {
            document.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(text => text.FontSize(9));
                page.Header().Column(header =>
                {
                    header.Item().Text("NALA - Historial consolidado de salud").Bold().FontSize(16).FontColor("#0c1a4e");
                    header.Item().Text($"{data.PetName} · Generado {data.GeneratedAt:dd/MM/yyyy HH:mm} UTC");
                    header.Item().Text("Registro administrativo; no constituye diagnóstico ni prescripción.")
                        .FontSize(8).FontColor("#6e5244");
                });
                page.Content().PaddingTop(12).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(75);
                        columns.ConstantColumn(83);
                        columns.RelativeColumn();
                    });
                    table.Header(header =>
                    {
                        foreach (var title in new[] { "Fecha", "Origen", "Detalle registrado" })
                            header.Cell().Background("#0c1a4e").Padding(5)
                                .Text(title).FontColor("#ffffff").Bold();
                    });
                    foreach (var item in data.Items)
                    {
                        var detail = item.Source == "Certificate"
                            ? $"{item.Label} · código {item.VerificationCode ?? "sin código"} · " +
                              (item.IsRevoked ? "Revocado" : "Emitido (verificar vigencia)")
                            : item.Label;
                        if (item.DocumentUrl is not null)
                            detail += $" · Adjunto: {item.DocumentKind ?? "sin clasificar"} (consulta autenticada en NALA)";
                        table.Cell().Padding(4).Text(item.Date.ToString("dd/MM/yyyy"));
                        table.Cell().Padding(4).Text(item.Source == "Certificate" ? "Certificado" : "Expediente");
                        table.Cell().Padding(4).Text(detail);
                    }
                });
                page.Footer().AlignCenter().Text(text =>
                {
                    text.CurrentPageNumber();
                    text.Span(" / ");
                    text.TotalPages();
                });
            });
        }).GeneratePdf();
        return Task.FromResult(bytes);
    }
}
