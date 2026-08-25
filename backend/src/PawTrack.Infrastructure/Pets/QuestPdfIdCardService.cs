using PawTrack.Application.Common.Interfaces;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using QRCoder;

namespace PawTrack.Infrastructure.Pets;

/// <summary>
/// Generates a printable A6 (105×148mm) pet identity card.
/// Contains: pet photo, name, species, breed, owner name, and a large QR code.
/// No PII beyond owner display name is embedded.
/// </summary>
public sealed class QuestPdfIdCardService : IPetIdCardService
{
    private const string Orange = "#E8521E";
    private const string DarkGrey = "#333333";
    private const string MidGrey = "#666666";
    private const string LightGrey = "#F5F5F5";

    public byte[] Generate(PetIdCardData data)
    {
        var qrBytes = GenerateQrPng(data.PublicProfileUrl);

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                // A6 — wallet-friendly, fits inside most laminators
                page.Size(105, 148, Unit.Millimetre);
                page.Margin(6, Unit.Millimetre);
                page.DefaultTextStyle(t => t.FontFamily(Fonts.Arial).FontSize(9));

                page.Content().Column(col =>
                {
                    // ── Header bar ─────────────────────────────────────────────
                    col.Item().Background(Orange).Padding(4).Row(row =>
                    {
                        row.RelativeItem().Text("PawTrack CR — ID de Mascota")
                            .Bold().FontSize(8).FontColor(Colors.White);
                    });

                    col.Item().PaddingTop(4).Row(row =>
                    {
                        // ── Left: photo ────────────────────────────────────────
                        row.ConstantItem(48, Unit.Millimetre).Column(photo =>
                        {
                            if (!string.IsNullOrWhiteSpace(data.PhotoUrl))
                            {
                                photo.Item().Height(48, Unit.Millimetre)
                                    .Image(data.PhotoUrl, ImageScaling.FitArea);
                            }
                            else
                            {
                                photo.Item().Height(48, Unit.Millimetre)
                                    .Background(LightGrey)
                                    .AlignCenter().AlignMiddle()
                                    .Text("Sin foto").FontSize(7).FontColor(MidGrey);
                            }
                        });

                        // ── Right: pet info ─────────────────────────────────────
                        row.RelativeItem().PaddingLeft(4).Column(info =>
                        {
                            info.Item().Text(data.PetName)
                                .Bold().FontSize(14).FontColor(DarkGrey);
                            info.Item().PaddingTop(2).Text(
                                $"{data.Species}{(data.Breed is not null ? $" · {data.Breed}" : "")}")
                                .FontSize(8).FontColor(MidGrey);
                            info.Item().PaddingTop(8).Text("Dueño/a:")
                                .FontSize(7).FontColor(MidGrey);
                            info.Item().Text(data.OwnerName)
                                .FontSize(9).Bold().FontColor(DarkGrey);
                        });
                    });

                    col.Item().PaddingTop(6).LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten2);

                    // ── QR section ──────────────────────────────────────────────
                    col.Item().PaddingTop(6).AlignCenter().Column(qr =>
                    {
                        qr.Item().AlignCenter().Width(52, Unit.Millimetre).Image(qrBytes);
                        qr.Item().PaddingTop(3).AlignCenter()
                            .Text("Escanea para ver perfil y contacto")
                            .FontSize(7).FontColor(MidGrey).Italic();
                    });

                    // ── Footer ──────────────────────────────────────────────────
                    col.Item().PaddingTop(6).AlignCenter()
                        .Text("pawtrack.cr")
                        .FontSize(7).FontColor(Orange).Bold();
                });
            });
        }).GeneratePdf();
    }

    private static byte[] GenerateQrPng(string url)
    {
        using var gen = new QRCodeGenerator();
        var codeData = gen.CreateQrCode(url, QRCodeGenerator.ECCLevel.M);
        using var code = new PngByteQRCode(codeData);
        return code.GetGraphic(6);
    }
}
