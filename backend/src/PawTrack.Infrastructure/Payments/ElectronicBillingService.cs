using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Payments.DTOs;
using PawTrack.Application.Payments.Interfaces;
using PawTrack.Domain.Common;
using PawTrack.Domain.Payments;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PawTrack.Infrastructure.Payments;

/// <summary>
/// Motor enterprise de Facturación Electrónica para Costa Rica (Hacienda DGT v4.3/v4.4).
/// Genera claves numéricas de 50 dígitos, consecutivos de 20 dígitos, almacena XMLs en Blob Storage
/// y produce representaciones gráficas PDF oficiales con QuestPDF.
/// </summary>
public sealed class ElectronicBillingService(
    IElectronicInvoiceRepository invoiceRepository,
    IUserBillingProfileRepository billingProfileRepository,
    IUserRepository userRepository,
    IBlobStorageService blobStorage,
    IUnitOfWork unitOfWork,
    IConfiguration configuration,
    ILogger<ElectronicBillingService> logger) : IElectronicBillingService
{
    static ElectronicBillingService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    private string EmisorCedula => configuration["Hacienda:EmisorCedula"] ?? "3101999999";
    private string EmisorNombre => configuration["Hacienda:EmisorNombre"] ?? "PAWTRACK COSTA RICA SOCIEDAD ANONIMA";
    private string Sucursal => configuration["Hacienda:Sucursal"] ?? "001";
    private string Terminal => configuration["Hacienda:Terminal"] ?? "00001";

    public async Task<Result<ElectronicInvoiceDto>> EmitInvoiceForTransactionAsync(
        EmitInvoiceRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
            return Result.Failure<ElectronicInvoiceDto>("Usuario no encontrado.");

        var billingProfile = await billingProfileRepository.GetByUserIdAsync(request.UserId, cancellationToken);

        // Si el usuario tiene perfil con "RequiresInvoice" o request.ForceInvoice = true, se emite Factura Electrónica (01)
        // De lo contrario, se emite Tiquete Electrónico (04) para consumidor final
        var isFactura = request.ForceInvoice || (billingProfile is not null && billingProfile.RequiresInvoice);
        var documentType = isFactura ? ElectronicInvoiceDocumentType.FacturaElectronica : ElectronicInvoiceDocumentType.TiqueteElectronico;
        var docTypeCode = isFactura ? "01" : "04";

        var sequence10 = await invoiceRepository.GetNextSequenceNumberAsync(documentType, cancellationToken);
        var consecutivo20 = $"{Sucursal}{Terminal}{docTypeCode}{sequence10}";

        var clave50 = GenerateClaveNumerica50(EmisorCedula, consecutivo20, DateTimeOffset.UtcNow);

        var receiverName = billingProfile?.LegalName ?? user.Name;
        var receiverEmail = billingProfile?.BillingEmail ?? user.Email;
        var receiverIdType = billingProfile?.IdentificationType;
        var receiverIdNumber = billingProfile?.IdentificationNumber;

        var invoice = ElectronicInvoice.Create(
            request.UserId,
            documentType,
            clave50,
            consecutivo20,
            request.CodigoCabys,
            request.Description,
            request.TotalAmountCrc,
            receiverName,
            receiverEmail,
            receiverIdType,
            receiverIdNumber,
            request.TransactionId,
            request.PaymentMethodCode);

        // Generar y almacenar XML estándar en Blob Storage
        var xmlContent = GenerateElectronicDocumentXml(invoice, EmisorNombre, EmisorCedula);
        var xmlBytes = System.Text.Encoding.UTF8.GetBytes(xmlContent);

        try
        {
            using var xmlStream = new MemoryStream(xmlBytes);
            var xmlBlobUrl = await blobStorage.UploadAsync(
                "invoices", $"{clave50}.xml", xmlStream, "application/xml", cancellationToken);
            invoice.MarkSigned(xmlBlobUrl);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to upload invoice XML blob for clave {Clave}", clave50);
        }

        // Generar y almacenar PDF oficial en Blob Storage
        try
        {
            var pdfBytes = GenerateInvoicePdfInternal(invoice, EmisorNombre, EmisorCedula);
            using var pdfStream = new MemoryStream(pdfBytes);
            var pdfBlobUrl = await blobStorage.UploadAsync(
                "invoices", $"{clave50}.pdf", pdfStream, "application/pdf", cancellationToken);
            invoice.SetPdfUrl(pdfBlobUrl);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to upload invoice PDF blob for clave {Clave}", clave50);
        }

        invoice.MarkAcceptedByHacienda(invoice.SignedXmlUrl ?? string.Empty);

        await invoiceRepository.AddAsync(invoice, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Emitted electronic {DocType} successfully. Clave: {Clave}, Amount: ₡{Amount}",
            docTypeCode, clave50, request.TotalAmountCrc);

        return Result.Success(ElectronicInvoiceDto.FromDomain(invoice));
    }

    public async Task<byte[]> GenerateInvoicePdfAsync(
        Guid invoiceId,
        CancellationToken cancellationToken = default)
    {
        var invoice = await invoiceRepository.GetByIdAsync(invoiceId, cancellationToken);
        if (invoice is null)
            throw new KeyNotFoundException("Factura electrónica no encontrada.");

        return GenerateInvoicePdfInternal(invoice, EmisorNombre, EmisorCedula);
    }

    private static string GenerateClaveNumerica50(string emisorCedula, string consecutivo20, DateTimeOffset fecha)
    {
        // 506 (3) + Día (2) + Mes (2) + Año (2) + Cédula Emisor (12) + Consecutivo (20) + Situación (1) + Seguridad (8) = 50 dígitos
        var pais = "506";
        var dia = fecha.Day.ToString("00");
        var mes = fecha.Month.ToString("00");
        var anio = (fecha.Year % 100).ToString("00");
        var cedula12 = emisorCedula.Replace("-", "").PadLeft(12, '0');
        var situacion = "1"; // 1 = Normal
        var seguridad = RandomNumberGenerator.GetInt32(10000000, 99999999).ToString("00000000");

        var clave = $"{pais}{dia}{mes}{anio}{cedula12}{consecutivo20}{situacion}{seguridad}";
        return clave.Length > 50 ? clave[..50] : clave.PadRight(50, '0');
    }

    private static string GenerateElectronicDocumentXml(ElectronicInvoice invoice, string emisorNombre, string emisorCedula)
    {
        var rootElement = invoice.DocumentType == ElectronicInvoiceDocumentType.FacturaElectronica
            ? "FacturaElectronica"
            : "TiqueteElectronico";

        return $"""
            <?xml version="1.0" encoding="utf-8"?>
            <{rootElement} xmlns="https://cdn.comprobanteselectronicos.go.cr/xml-schemas/v4.3/{rootElement.ToLowerInvariant()}">
              <Clave>{invoice.ClaveNumerica}</Clave>
              <CodigoActividad>831410</CodigoActividad>
              <NumeroConsecutivo>{invoice.NumeroConsecutivo}</NumeroConsecutivo>
              <FechaEmision>{invoice.IssuedAt:yyyy-MM-ddTHH:mm:sszzz}</FechaEmision>
              <Emisor>
                <Nombre>{emisorNombre}</Nombre>
                <Identificacion>
                  <Tipo>02</Tipo>
                  <Numero>{emisorCedula}</Numero>
                </Identificacion>
                <CorreoElectronico>facturacion@pawtrack.cr</CorreoElectronico>
              </Emisor>
              <Receptor>
                <Nombre>{invoice.ReceiverName}</Nombre>
                {(invoice.ReceiverIdNumber is not null ? $"<Identificacion><Tipo>0{(int)(invoice.ReceiverIdType ?? TaxIdentificationType.Fisica)}</Tipo><Numero>{invoice.ReceiverIdNumber}</Numero></Identificacion>" : "")}
                <CorreoElectronico>{invoice.ReceiverEmail}</CorreoElectronico>
              </Receptor>
              <CondicionVenta>01</CondicionVenta>
              <MedioPago>{invoice.PaymentMethodCode}</MedioPago>
              <DetalleServicio>
                <LineaDetalle>
                  <NumeroLinea>1</NumeroLinea>
                  <CodigoCabys>{invoice.CodigoCabys}</CodigoCabys>
                  <Detalle>{invoice.ServiceDescription}</Detalle>
                  <PrecioUnitario>{invoice.SubtotalCrc:F2}</PrecioUnitario>
                  <MontoTotal>{invoice.SubtotalCrc:F2}</MontoTotal>
                  <SubTotal>{invoice.SubtotalCrc:F2}</SubTotal>
                  <Impuesto>
                    <Codigo>01</Codigo>
                    <Tarifa>13.00</Tarifa>
                    <Monto>{invoice.IvaAmountCrc:F2}</Monto>
                  </Impuesto>
                  <MontoTotalLinea>{invoice.TotalAmountCrc:F2}</MontoTotalLinea>
                </LineaDetalle>
              </DetalleServicio>
              <ResumenFactura>
                <CodigoTipoMoneda>CRC</CodigoTipoMoneda>
                <TotalServGravados>{invoice.SubtotalCrc:F2}</TotalServGravados>
                <TotalGravado>{invoice.SubtotalCrc:F2}</TotalGravado>
                <TotalVenta>{invoice.SubtotalCrc:F2}</TotalVenta>
                <TotalVentaNeta>{invoice.SubtotalCrc:F2}</TotalVentaNeta>
                <TotalImpuesto>{invoice.IvaAmountCrc:F2}</TotalImpuesto>
                <TotalComprobante>{invoice.TotalAmountCrc:F2}</TotalComprobante>
              </ResumenFactura>
            </{rootElement}>
            """;
    }

    private static byte[] GenerateInvoicePdfInternal(ElectronicInvoice invoice, string emisorNombre, string emisorCedula)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var docTypeName = invoice.DocumentType == ElectronicInvoiceDocumentType.FacturaElectronica
            ? "FACTURA ELECTRÓNICA"
            : "TIQUETE ELECTRÓNICO";

        var pdfDoc = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.Letter);
                page.Margin(36);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                page.Header().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("PAWTRACK CR").FontSize(20).Bold().FontColor("#E8521E");
                            c.Item().Text(emisorNombre).FontSize(11).Bold();
                            c.Item().Text($"Cédula Jurídica: {emisorCedula}").FontSize(9);
                            c.Item().Text("San José, Costa Rica · facturacion@pawtrack.cr").FontSize(9).FontColor("#6B7280");
                        });

                        row.ConstantItem(220).Column(c =>
                        {
                            c.Item().Border(1).BorderColor("#E5E7EB").Padding(8).Column(box =>
                            {
                                box.Item().Text(docTypeName).FontSize(12).Bold().FontColor("#1F2937");
                                box.Item().Text($"Consecutivo: {invoice.NumeroConsecutivo}").FontSize(9).Bold();
                                box.Item().Text($"Fecha: {invoice.IssuedAt:dd/MM/yyyy HH:mm:ss}").FontSize(8);
                                box.Item().Text("Versión DGT: 4.3").FontSize(8).FontColor("#9CA3AF");
                            });
                        });
                    });

                    col.Item().PaddingTop(12).BorderBottom(1).BorderColor("#E5E7EB");
                });

                page.Content().PaddingVertical(14).Column(col =>
                {
                    // Clave numérica oficial
                    col.Item().Background("#F9FAFB").Padding(6).Column(c =>
                    {
                        c.Item().Text("CLAVE NUMÉRICA (50 DÍGITOS)").FontSize(8).Bold().FontColor("#4B5563");
                        c.Item().Text(invoice.ClaveNumerica).FontSize(8).FontFamily("Courier New").Bold();
                    });

                    // Receptor
                    col.Item().PaddingTop(12).Row(r =>
                    {
                        r.RelativeItem().Column(c =>
                        {
                            c.Item().Text("RECEPTOR").FontSize(10).Bold().FontColor("#1F2937");
                            c.Item().Text($"Nombre: {invoice.ReceiverName}").FontSize(9);
                            if (!string.IsNullOrEmpty(invoice.ReceiverIdNumber))
                                c.Item().Text($"Identificación: {invoice.ReceiverIdNumber} ({invoice.ReceiverIdType})").FontSize(9);
                            c.Item().Text($"Correo: {invoice.ReceiverEmail}").FontSize(9);
                        });

                        r.ConstantItem(200).Column(c =>
                        {
                            c.Item().Text("CONDICIONES").FontSize(10).Bold().FontColor("#1F2937");
                            c.Item().Text("Condición de venta: Contado").FontSize(9);
                            c.Item().Text($"Medio de pago: {(invoice.PaymentMethodCode == "02" ? "Tarjeta Débito/Crédito" : "SINPE Móvil")}").FontSize(9);
                            c.Item().Text("Moneda: CRC (Colón costarricense)").FontSize(9);
                        });
                    });

                    // Tabla de líneas
                    col.Item().PaddingTop(16).Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.ConstantColumn(85);
                            cols.RelativeColumn();
                            cols.ConstantColumn(75);
                            cols.ConstantColumn(65);
                            cols.ConstantColumn(80);
                        });

                        table.Header(h =>
                        {
                            h.Cell().Background("#F3F4F6").Padding(5).Text("CABYS").Bold().FontSize(8);
                            h.Cell().Background("#F3F4F6").Padding(5).Text("Descripción").Bold().FontSize(8);
                            h.Cell().Background("#F3F4F6").Padding(5).AlignRight().Text("Subtotal").Bold().FontSize(8);
                            h.Cell().Background("#F3F4F6").Padding(5).AlignRight().Text("IVA (13%)").Bold().FontSize(8);
                            h.Cell().Background("#F3F4F6").Padding(5).AlignRight().Text("Total").Bold().FontSize(8);
                        });

                        table.Cell().BorderBottom(1).BorderColor("#F3F4F6").Padding(5).Text(invoice.CodigoCabys).FontSize(8);
                        table.Cell().BorderBottom(1).BorderColor("#F3F4F6").Padding(5).Text(invoice.ServiceDescription).FontSize(8);
                        table.Cell().BorderBottom(1).BorderColor("#F3F4F6").Padding(5).AlignRight().Text($"₡{invoice.SubtotalCrc:N2}").FontSize(8);
                        table.Cell().BorderBottom(1).BorderColor("#F3F4F6").Padding(5).AlignRight().Text($"₡{invoice.IvaAmountCrc:N2}").FontSize(8);
                        table.Cell().BorderBottom(1).BorderColor("#F3F4F6").Padding(5).AlignRight().Text($"₡{invoice.TotalAmountCrc:N2}").FontSize(8).Bold();
                    });

                    // Totales
                    col.Item().PaddingTop(12).AlignRight().Row(r =>
                    {
                        r.ConstantItem(220).Column(t =>
                        {
                            t.Item().Row(tr => { tr.RelativeItem().Text("Subtotal Neto:"); tr.ConstantItem(80).AlignRight().Text($"₡{invoice.SubtotalCrc:N2}"); });
                            t.Item().Row(tr => { tr.RelativeItem().Text("IVA 13%:"); tr.ConstantItem(80).AlignRight().Text($"₡{invoice.IvaAmountCrc:N2}"); });
                            t.Item().BorderTop(1).BorderColor("#D1D5DB").PaddingTop(4).Row(tr =>
                            {
                                tr.RelativeItem().Text("TOTAL A PAGAR:").Bold().FontSize(11);
                                tr.ConstantItem(80).AlignRight().Text($"₡{invoice.TotalAmountCrc:N2}").Bold().FontSize(11).FontColor("#E8521E");
                            });
                        });
                    });
                });

                page.Footer().Column(col =>
                {
                    col.Item().BorderTop(1).BorderColor("#E5E7EB").PaddingTop(8);
                    col.Item().Text("Autorizada mediante resolución de la DGT N° DGT-R-48-2016 del 7 de octubre de 2016 de la Dirección General de Tributación.")
                        .FontSize(7).FontColor("#6B7280").AlignCenter();
                    col.Item().Text("Comprobante electrónico emitido por la plataforma oficial PawTrack CR · https://pawtrack.cr")
                        .FontSize(7).FontColor("#9CA3AF").AlignCenter();
                });
            });
        });

        return pdfDoc.GeneratePdf();
    }
}
