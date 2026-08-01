using PawTrack.Application.Medical;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PawTrack.Infrastructure.Medical;

public sealed class QuestPdfMedicalExporter : IMedicalPdfExporter
{
    public Task<byte[]> ExportAsync(
        string petName,
        IReadOnlyList<MedicalRecordDto> records,
        IReadOnlyList<VetReminderDto> reminders,
        CancellationToken ct = default)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(t => t.FontSize(10));

                page.Header().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("PawTrack CR").Bold().FontSize(18).FontColor("#e8521e");
                            c.Item().Text($"Historial Médico — {petName}").FontSize(14).Bold();
                            c.Item().Text($"Generado: {DateTimeOffset.UtcNow:dd/MM/yyyy HH:mm} UTC")
                                .FontSize(8).FontColor("#888888");
                        });
                    });
                    col.Item().PaddingTop(8).LineHorizontal(1).LineColor("#e8521e");
                });

                page.Content().PaddingTop(16).Column(col =>
                {
                    col.Item().PaddingBottom(6).Text("Registros médicos").Bold().FontSize(13);

                    if (records.Count == 0)
                    {
                        col.Item().Text("No hay registros médicos aún.").FontColor("#888888");
                    }
                    else
                    {
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.ConstantColumn(70);
                                c.ConstantColumn(80);
                                c.RelativeColumn();
                                c.ConstantColumn(80);
                                c.ConstantColumn(80);
                            });

                            table.Header(h =>
                            {
                                foreach (var header in new[] { "Tipo", "Fecha", "Descripción", "Veterinario", "Clínica" })
                                    h.Cell().Background("#0c1a4e").Padding(5)
                                        .Text(header).FontColor("#ffffff").Bold().FontSize(9);
                            });

                            for (var i = 0; i < records.Count; i++)
                            {
                                var r = records[i];
                                var bg = i % 2 == 0 ? "#ffffff" : "#f9f5ef";
                                table.Cell().Background(bg).Padding(4).Text(r.Type).FontSize(9);
                                table.Cell().Background(bg).Padding(4).Text(r.Date.ToString("dd/MM/yyyy")).FontSize(9);
                                table.Cell().Background(bg).Padding(4).Text(r.Description).FontSize(9);
                                table.Cell().Background(bg).Padding(4).Text(r.VetName ?? "—").FontSize(9);
                                table.Cell().Background(bg).Padding(4).Text(r.ClinicName ?? "—").FontSize(9);
                            }
                        });
                    }

                    var pending = reminders.Where(r => !r.IsCompleted).OrderBy(r => r.DueDate).ToList();
                    if (pending.Count > 0)
                    {
                        col.Item().PaddingTop(20).PaddingBottom(6).Text("Recordatorios pr\u00f3ximos").Bold().FontSize(13);
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.ConstantColumn(80);
                                c.ConstantColumn(80);
                                c.RelativeColumn();
                            });
                            table.Header(h =>
                            {
                                foreach (var hdr in new[] { "Tipo", "Fecha", "Título" })
                                    h.Cell().Background("#0c1a4e").Padding(5)
                                        .Text(hdr).FontColor("#ffffff").Bold().FontSize(9);
                            });
                            foreach (var r in pending)
                            {
                                table.Cell().Padding(4).Text(r.Type).FontSize(9);
                                table.Cell().Padding(4).Text(r.DueDate.ToString("dd/MM/yyyy")).FontSize(9);
                                table.Cell().Padding(4).Text(r.Title).FontSize(9);
                            }
                        });
                    }
                });

                page.Footer().AlignCenter().Text(t =>
                {
                    t.Span("PawTrack CR · pawtrack.cr · ").FontSize(8).FontColor("#888888");
                    t.CurrentPageNumber().FontSize(8).FontColor("#888888");
                    t.Span(" / ").FontSize(8).FontColor("#888888");
                    t.TotalPages().FontSize(8).FontColor("#888888");
                });
            });
        });

        return Task.FromResult(doc.GeneratePdf());
    }
}

