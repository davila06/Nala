using PawTrack.Application.Certificates.Interfaces;
using PawTrack.Application.Common.Interfaces;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using QRCoder;

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
        // VaccinePassport uses its own bilingual OIRSA layout
        if (data.CertificateType == "VaccinePassport")
            return GeneratePassportPdf(data);

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
                            c.Item().Text("CERTIFICADO VERIFICABLE")
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
                    col.Item().PaddingTop(6).Row(row =>
                    {
                        row.RelativeItem().Text(text =>
                        {
                            text.Span("Verifique la autenticidad en ")
                                .FontSize(8).FontColor(Colors.Grey.Medium);
                            text.Span($"pawtrack.cr/verificar/{data.VerificationCode}")
                                .FontSize(8).Bold().FontColor(Colors.Orange.Medium);
                        });
                        // Embed a scannable QR code pointing to the public verification URL
                        row.ConstantItem(48).AlignRight().Image(
                            GenerateQrPng($"https://pawtrack.cr/verificar/{data.VerificationCode}"));
                    });
                });
            });
        }).GeneratePdf();
    }

    private static byte[] GenerateQrPng(string url)
    {
        using var generator = new QRCodeGenerator();
        var data = generator.CreateQrCode(url, QRCodeGenerator.ECCLevel.Q);
        using var code = new PngByteQRCode(data);
        return code.GetGraphic(4);
    }

    // ── OIRSA Vaccine Passport ────────────────────────────────────────────────

    private static byte[] GeneratePassportPdf(CertificatePdfData d)
    {
        const string Navy = "#0c1a4e";
        const string Orange = "#e8521e";
        const string Sand = "#f9f5ef";
        const string Green = "#17a26d";
        const string Gray = "#6e5244";

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.8f, Unit.Centimetre);
                page.DefaultTextStyle(t => t.FontSize(10).FontFamily("Arial"));

                page.Header().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("CERTIFICADO DE SALUD / HEALTH CERTIFICATE")
                                .Bold().FontSize(14).FontColor(Navy);
                            c.Item().Text("Formato SENASA-ready / OIRSA-compatible")
                                .FontSize(9).FontColor(Gray);
                            c.Item().PaddingTop(2).Text($"PawTrack CR · pawtrack.cr · {d.IssuedAt:dd/MM/yyyy}")
                                .FontSize(8).FontColor(Gray);
                        });
                        if (!string.IsNullOrEmpty(d.VerificationCode))
                        {
                            row.ConstantItem(70).Column(c =>
                            {
                                var qr = GenerateQrPng($"https://pawtrack.cr/verificar/{d.VerificationCode}");
                                c.Item().Image(qr);
                                c.Item().Text(d.VerificationCode).FontSize(7).FontColor(Gray).AlignCenter();
                            });
                        }
                    });
                    col.Item().PaddingTop(6).LineHorizontal(2).LineColor(Orange);
                });

                page.Content().PaddingTop(14).Column(col =>
                {
                    // ── Sección I: Identificación del animal ─────────────────
                    SectionHeader(col, "Sección I: Identificación del animal / Animal Identification", Navy, Orange);
                    col.Item().Background(Sand).Padding(8).Table(table =>
                    {
                        table.ColumnsDefinition(c => { c.RelativeColumn(); c.RelativeColumn(); });
                        void Row2(string l1, string v1, string l2, string v2)
                        {
                            table.Cell().Padding(3).Column(c => { c.Item().Text(l1).FontSize(8).FontColor(Gray); c.Item().Text(v1).Bold().FontSize(10); });
                            table.Cell().Padding(3).Column(c => { c.Item().Text(l2).FontSize(8).FontColor(Gray); c.Item().Text(v2).Bold().FontSize(10); });
                        }
                        Row2("Nombre / Name:", d.PetName, "Especie / Species:", d.PetSpecies);
                        Row2("Raza / Breed:", d.PetBreed ?? "—", "Color:", d.PetColor ?? "—");
                        Row2("Microchip:", d.MicrochipId ?? "—", "Propietario / Owner:", d.OwnerName ?? "—");
                    });
                    col.Item().PaddingBottom(10);

                    // ── Sección II: Vacunaciones ─────────────────────────────
                    SectionHeader(col, "Sección II: Vacunaciones / Vaccinations", Navy, Orange);
                    if (d.Vaccines?.Count > 0)
                    {
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(2);  // vaccine
                                c.RelativeColumn(1.2f); // brand
                                c.ConstantColumn(55); // lot
                                c.ConstantColumn(60); // date
                                c.ConstantColumn(60); // valid
                            });
                            table.Header(h =>
                            {
                                foreach (var txt in new[] { "Vacuna / Vaccine", "Marca / Brand", "Lote / Lot", "Fecha / Date", "Válido hasta" })
                                    h.Cell().Background(Navy).Padding(4)
                                        .Text(txt).FontColor("#ffffff").Bold().FontSize(8);
                            });
                            for (var i = 0; i < d.Vaccines.Count; i++)
                            {
                                var v = d.Vaccines[i];
                                var bg = i % 2 == 0 ? "#ffffff" : Sand;
                                table.Cell().Background(bg).Padding(3).Text(v.VaccineName).FontSize(9);
                                table.Cell().Background(bg).Padding(3).Text(v.Brand ?? "—").FontSize(9);
                                table.Cell().Background(bg).Padding(3).Text(v.LotNumber ?? "—").FontSize(9);
                                table.Cell().Background(bg).Padding(3).Text(v.ApplicationDate.ToString("dd/MM/yy")).FontSize(9);
                                table.Cell().Background(bg).Padding(3).Text(v.ValidUntil?.ToString("dd/MM/yy") ?? "—").FontSize(9);
                            }
                        });
                    }
                    else
                    {
                        col.Item().Text("Sin vacunas registradas.").FontSize(9).FontColor(Gray);
                    }
                    col.Item().PaddingBottom(10);

                    // ── Sección III: Control de parásitos ───────────────────
                    SectionHeader(col, "Sección III: Control de parásitos / Parasite Control", Navy, Orange);
                    if (d.ParasiteControl is { } pc)
                    {
                        col.Item().Background(Sand).Padding(8).Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("Producto:").FontSize(8).FontColor(Gray);
                                c.Item().Text(pc.ProductName).Bold().FontSize(10);
                            });
                            row.ConstantItem(10);
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("Fecha de aplicación:").FontSize(8).FontColor(Gray);
                                c.Item().Text(pc.ApplicationDate.ToString("dd/MM/yyyy")).Bold();
                            });
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("Próxima dosis:").FontSize(8).FontColor(Gray);
                                c.Item().Text(pc.NextDueDate?.ToString("dd/MM/yyyy") ?? "—").Bold();
                            });
                        });
                    }
                    else
                    {
                        col.Item().Text("No registrado.").FontSize(9).FontColor(Gray);
                    }
                    col.Item().PaddingBottom(12);

                    // ── Sección IV: Firma clínica ────────────────────────────
                    col.Item().Background(Navy).Padding(10).Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text(d.ClinicName).Bold().FontSize(11).FontColor("#ffffff");
                            c.Item().Text($"SENASA: {d.ClinicLicense}").FontSize(8).FontColor("#a4e9cb");
                        });
                        row.RelativeItem().AlignRight().Column(c =>
                        {
                            c.Item().Text($"Médico veterinario: {d.VetName}").FontSize(9).FontColor("#ffffff").AlignRight();
                            c.Item().Text($"Emitido: {d.IssuedAt:dd/MM/yyyy}").FontSize(8).FontColor("#a4e9cb").AlignRight();
                        });
                    });
                });

                page.Footer().Column(col =>
                {
                    col.Item().LineHorizontal(1).LineColor("#e2d3c4");
                    col.Item().PaddingTop(4).Row(row =>
                    {
                        row.RelativeItem().Text($"Verificar en: pawtrack.cr/verificar/{d.VerificationCode}").FontSize(7).FontColor(Gray);
                        row.RelativeItem().Text("Emitido digitalmente por PawTrack CR").FontSize(7).FontColor(Gray).AlignRight();
                    });
                });
            });
        }).GeneratePdf();
    }

    private static void SectionHeader(ColumnDescriptor col, string title, string navy, string accent)
    {
        col.Item().PaddingBottom(4).Row(row =>
        {
            row.ConstantItem(4).Background(accent).Height(16);
            row.RelativeItem().PaddingLeft(8).Text(title).Bold().FontSize(10).FontColor(navy);
        });
    }
}
