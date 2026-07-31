using PawTrack.Application.Certificates.Interfaces;
using PawTrack.Application.Common.Interfaces;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PawTrack.Infrastructure.Certificates;

/// <summary>Generates a PDF/A certificate with QuestPDF and stores it in Azure Blob Storage.</summary>
public sealed class QuestPdfCertificateService(IBlobStorageService blobStorage) : ICertificateService
{
    static QuestPdfCertificateService()
    {
        // Community license — free for companies with revenue < $1M/year
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public async Task<string> GenerateAndStoreAsync(
        CertificatePdfData data,
        CancellationToken cancellationToken = default)
    {
        var pdfBytes = GeneratePdf(data);
        var blobName = $"{data.CertificateId}.pdf";

        using var stream = new MemoryStream(pdfBytes);
        var url = await blobStorage.UploadAsync(
            "certificates",
            blobName,
            stream,
            "application/pdf",
            cancellationToken);

        return url;
    }

    private static byte[] GeneratePdf(CertificatePdfData data)
    {
        var typeLabel = data.CertificateType switch
        {
            "Vaccination" => "Certificado de Vacunación",
            "GeneralExam" => "Certificado de Examen General",
            "Deworming" => "Certificado de Desparasitación",
            "Neutering" => "Certificado de Esterilización",
            "HealthClearance" => "Certificado de Salud",
            "MicrochipRegistration" => "Registro de Microchip",
            _ => data.CertificateType,
        };

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(11).FontFamily(Fonts.Arial));

                page.Header().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("PawTrack CR")
                                .Bold().FontSize(22).FontColor(Colors.Orange.Medium);
                            c.Item().Text("Plataforma de identidad veterinaria")
                                .FontSize(9).FontColor(Colors.Grey.Darken1);
                        });
                        row.ConstantItem(120).AlignRight().Column(c =>
                        {
                            c.Item().Text("CERTIFICADO OFICIAL")
                                .Bold().FontSize(10).FontColor(Colors.Grey.Darken2);
                            c.Item().Text($"N° {data.VerificationCode}")
                                .FontSize(9).FontColor(Colors.Grey.Medium);
                        });
                    });
                    col.Item().PaddingTop(6).LineHorizontal(1.5f).LineColor(Colors.Orange.Medium);
                });

                page.Content().PaddingVertical(20).Column(col =>
                {
                    // Certificate type title
                    col.Item().PaddingBottom(16).Text(typeLabel)
                        .Bold().FontSize(18).FontColor(Colors.Grey.Darken3).AlignCenter();

                    // Pet info section
                    col.Item().Background(Colors.Grey.Lighten4).Padding(12).Column(section =>
                    {
                        section.Item().Text("Información de la Mascota").Bold().FontSize(10);
                        section.Item().PaddingTop(6).Row(row =>
                        {
                            row.RelativeItem().Text($"Nombre: {data.PetName}");
                            row.RelativeItem().Text($"Especie: {data.PetSpecies}");
                            row.RelativeItem().Text($"Raza: {data.PetBreed ?? "No especificada"}");
                        });
                    });

                    col.Item().PaddingTop(12);

                    // Clinic info section
                    col.Item().Background(Colors.Grey.Lighten4).Padding(12).Column(section =>
                    {
                        section.Item().Text("Emitido por").Bold().FontSize(10);
                        section.Item().PaddingTop(6).Row(row =>
                        {
                            row.RelativeItem().Text($"Clínica: {data.ClinicName}");
                            row.RelativeItem().Text($"Licencia SENASA: {data.ClinicLicense}");
                        });
                        section.Item().PaddingTop(4).Text($"Médico Veterinario: {data.VetName}");
                    });

                    // Dates
                    col.Item().PaddingTop(12).Row(row =>
                    {
                        row.RelativeItem().Text($"Fecha de emisión: {data.IssuedAt:dd/MM/yyyy}");
                        row.RelativeItem().Text(data.ValidUntil.HasValue
                            ? $"Válido hasta: {data.ValidUntil:dd/MM/yyyy}"
                            : "Sin fecha de vencimiento");
                    });

                    // Notes
                    if (!string.IsNullOrWhiteSpace(data.Notes))
                    {
                        col.Item().PaddingTop(12).Column(n =>
                        {
                            n.Item().Text("Observaciones:").Bold().FontSize(10);
                            n.Item().PaddingTop(4).Text(data.Notes).FontColor(Colors.Grey.Darken1);
                        });
                    }

                    // Signature area
                    col.Item().PaddingTop(32).Row(row =>
                    {
                        row.RelativeItem().Column(sig =>
                        {
                            sig.Item().LineHorizontal(0.5f).LineColor(Colors.Grey.Medium);
                            sig.Item().PaddingTop(4).Text("Firma del Médico Veterinario")
                                .FontSize(9).FontColor(Colors.Grey.Medium).AlignCenter();
                        });
                        row.ConstantItem(40);
                        row.RelativeItem().Column(sig =>
                        {
                            sig.Item().LineHorizontal(0.5f).LineColor(Colors.Grey.Medium);
                            sig.Item().PaddingTop(4).Text("Sello de la Clínica")
                                .FontSize(9).FontColor(Colors.Grey.Medium).AlignCenter();
                        });
                    });
                });

                page.Footer().AlignCenter().Column(col =>
                {
                    col.Item().LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten2);
                    col.Item().PaddingTop(6).Text(text =>
                    {
                        text.Span("Verifique la autenticidad de este certificado en ")
                            .FontSize(8).FontColor(Colors.Grey.Medium);
                        text.Span($"pawtrack.cr/verificar/{data.VerificationCode}")
                            .FontSize(8).Bold().FontColor(Colors.Orange.Medium);
                    });
                });
            });
        }).GeneratePdf();
    }
}
