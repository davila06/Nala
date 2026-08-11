using PawTrack.Application.Medical;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PawTrack.Infrastructure.Medical;

public sealed class QuestPdfAnnualReportGenerator : IAnnualReportPdfGenerator
{
    private const string BrandOrange = "#e8521e";
    private const string TrustNavy = "#0c1a4e";
    private const string SandLight = "#f9f5ef";
    private const string TextGray = "#6e5244";
    private const string RescueGreen = "#17a26d";

    public Task<byte[]> GenerateAsync(AnnualReportData data, CancellationToken ct = default)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var bytes = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.8f, Unit.Centimetre);
                page.DefaultTextStyle(t => t.FontSize(10).FontFamily("Arial"));

                // ── Header ───────────────────────────────────────────────────
                page.Header().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text($"PawTrack CR — Informe Anual {data.Year}")
                                .Bold().FontSize(18).FontColor(BrandOrange);
                            c.Item().Text(data.PetName)
                                .Bold().FontSize(14).FontColor(TrustNavy);
                            c.Item().PaddingTop(2)
                                .Text($"Generado: {DateTimeOffset.UtcNow:dd/MM/yyyy}")
                                .FontSize(8).FontColor(TextGray);
                        });
                        row.ConstantItem(90).AlignRight().AlignMiddle().Column(c =>
                        {
                            c.Item().Background(SandLight).Padding(8).AlignCenter().Column(inner =>
                            {
                                inner.Item().Text(SpeciesEmoji(data.Species)).FontSize(28).AlignCenter();
                                inner.Item().Text(data.Species).FontSize(8).FontColor(TextGray).AlignCenter();
                            });
                        });
                    });
                    col.Item().PaddingTop(6).LineHorizontal(2).LineColor(BrandOrange);
                });

                page.Content().PaddingTop(14).Column(col =>
                {
                    // ── 1. Resumen del año ───────────────────────────────────
                    col.Item().Background(TrustNavy).Padding(10).Row(row =>
                    {
                        Stat(row, data.VetVisits.Count.ToString(), "Visitas\nveterinarias");
                        Stat(row, data.TotalQrScans.ToString(), "Escaneos\nde QR");
                        Stat(row, data.LostEvents.Count == 0 ? "✓" : data.LostEvents.Count.ToString(),
                            data.LostEvents.Count == 0 ? "Sin eventos\nde pérdida" : "Evento(s)\nde pérdida");
                        var ageText = data.AgeMonths.HasValue
                            ? data.AgeMonths.Value >= 12
                                ? $"{data.AgeMonths.Value / 12} a {data.AgeMonths.Value % 12} m"
                                : $"{data.AgeMonths.Value} meses"
                            : "—";
                        Stat(row, ageText, "Edad actual");
                    });
                    col.Item().PaddingBottom(12);

                    // ── 2. Visitas veterinarias ───────────────────────────────
                    if (data.VetVisits.Count > 0)
                    {
                        SectionHeader(col, "Visitas Veterinarias");
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.ConstantColumn(70); // date
                                c.ConstantColumn(80); // type
                                c.RelativeColumn();   // description
                                c.ConstantColumn(90); // clinic
                            });
                            table.Header(h =>
                            {
                                foreach (var txt in new[] { "Fecha", "Tipo", "Descripción", "Clínica" })
                                    h.Cell().Background(TrustNavy).Padding(5)
                                        .Text(txt).FontColor("#ffffff").Bold().FontSize(9);
                            });
                            for (var i = 0; i < data.VetVisits.Count; i++)
                            {
                                var v = data.VetVisits[i];
                                var bg = i % 2 == 0 ? "#ffffff" : SandLight;
                                table.Cell().Background(bg).Padding(4).Text(v.Date.ToString("dd/MM/yy")).FontSize(9);
                                table.Cell().Background(bg).Padding(4).Text(v.Type).FontSize(9);
                                table.Cell().Background(bg).Padding(4)
                                    .Text(v.Description.Length > 60 ? v.Description[..60] + "…" : v.Description)
                                    .FontSize(9);
                                table.Cell().Background(bg).Padding(4).Text(v.ClinicName ?? "—").FontSize(9);
                            }
                        });
                        col.Item().PaddingBottom(12);
                    }

                    // ── 3. Peso ───────────────────────────────────────────────
                    if (data.WeightSummary is { } ws)
                    {
                        SectionHeader(col, "Evolución de Peso");
                        col.Item().Background(SandLight).Padding(10).Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text($"Inicio del período ({ws.FirstDate:dd/MM/yy}):").FontSize(9).FontColor(TextGray);
                                c.Item().Text($"{ws.FirstKg:F1} kg").Bold().FontSize(14).FontColor(TrustNavy);
                            });
                            row.ConstantItem(40).AlignCenter().AlignMiddle()
                                .Text(ws.LastKg > ws.FirstKg ? "▲" : ws.LastKg < ws.FirstKg ? "▼" : "=")
                                .FontSize(20).FontColor(ws.LastKg > ws.FirstKg ? BrandOrange : ws.LastKg < ws.FirstKg ? "#d42020" : RescueGreen);
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text($"Fin del período ({ws.LastDate:dd/MM/yy}):").FontSize(9).FontColor(TextGray).AlignRight();
                                c.Item().Text($"{ws.LastKg:F1} kg").Bold().FontSize(14)
                                    .FontColor(ws.LastKg > ws.FirstKg ? BrandOrange : RescueGreen).AlignRight();
                            });
                        });
                        var delta = ws.LastKg - ws.FirstKg;
                        col.Item().PaddingTop(4)
                            .Text($"Cambio neto: {(delta >= 0 ? "+" : "")}{delta:F1} kg ({Math.Abs((delta / ws.FirstKg) * 100):F0}%)")
                            .FontSize(9).FontColor(TextGray);
                        col.Item().PaddingBottom(12);
                    }

                    // ── 4. Eventos de pérdida ────────────────────────────────
                    if (data.LostEvents.Count > 0)
                    {
                        SectionHeader(col, "Eventos de Pérdida");
                        foreach (var evt in data.LostEvents)
                        {
                            col.Item().Background(evt.Reunited ? "#f0fdf6" : "#fff8f4")
                                .Border(1).BorderColor(evt.Reunited ? "#a4e9cb" : "#ffd0b4")
                                .Padding(8).Row(row =>
                            {
                                row.ConstantItem(30).AlignCenter().AlignMiddle()
                                    .Text(evt.Reunited ? "✅" : "⚠️").FontSize(18);
                                row.RelativeItem().Column(c =>
                                {
                                    c.Item().Text($"Reportado: {evt.ReportedDate:dd/MM/yyyy}").FontSize(9).Bold();
                                    if (evt.ResolvedDate.HasValue)
                                        c.Item().Text($"Resuelto: {evt.ResolvedDate.Value:dd/MM/yyyy}  •  {evt.DaysLost} día(s)")
                                            .FontSize(9).FontColor(TextGray);
                                    c.Item().Text(evt.Reunited ? "Reunificado con éxito" : "En progreso / sin resolver")
                                        .FontSize(9).FontColor(evt.Reunited ? RescueGreen : BrandOrange);
                                });
                            });
                        }
                        col.Item().PaddingBottom(12);
                    }
                    else
                    {
                        col.Item().Background("#f0fdf6").Border(1).BorderColor("#a4e9cb")
                            .Padding(10).Row(row =>
                        {
                            row.ConstantItem(24).Text("✅").FontSize(16);
                            row.RelativeItem().Text($"{data.PetName} no tuvo eventos de pérdida en {data.Year}. ¡Excelente!")
                                .FontSize(10).FontColor(RescueGreen);
                        });
                        col.Item().PaddingBottom(12);
                    }
                });

                // ── Footer ───────────────────────────────────────────────────
                page.Footer().Column(col =>
                {
                    col.Item().LineHorizontal(1).LineColor("#e2d3c4");
                    col.Item().PaddingTop(6).Row(row =>
                    {
                        row.RelativeItem().Text("PawTrack CR · pawtrack.cr").FontSize(8).FontColor(TextGray);
                        row.RelativeItem().Text($"Informe {data.Year} — {data.PetName}").FontSize(8).FontColor(TextGray).AlignRight();
                    });
                });
            });
        }).GeneratePdf();

        return Task.FromResult(bytes);
    }

    private static void SectionHeader(ColumnDescriptor col, string title)
    {
        col.Item().PaddingBottom(4).Row(row =>
        {
            row.ConstantItem(4).Background(BrandOrange).Height(16);
            row.RelativeItem().PaddingLeft(8).Text(title).Bold().FontSize(11).FontColor(TrustNavy);
        });
    }

    private static void Stat(RowDescriptor row, string value, string label)
    {
        row.RelativeItem().AlignCenter().Column(c =>
        {
            c.Item().Text(value).Bold().FontSize(20).FontColor("#ffffff").AlignCenter();
            c.Item().Text(label).FontSize(7).FontColor("#a4e9cb").AlignCenter();
        });
    }

    private static string SpeciesEmoji(string species) => species switch
    {
        "Dog" => "🐶",
        "Cat" => "🐱",
        "Bird" => "🐦",
        "Rabbit" => "🐰",
        _ => "🐾",
    };
}
