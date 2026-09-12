namespace PawTrack.Domain.Payments;

public enum ElectronicInvoiceDocumentType
{
    FacturaElectronica = 1, // Tipo 01: Factura para crédito fiscal
    NotaDebito = 2,          // Tipo 02: Nota de Débito
    NotaCredito = 3,         // Tipo 03: Nota de Crédito
    TiqueteElectronico = 4,  // Tipo 04: Consumidor final sin crédito fiscal
}

public enum ElectronicInvoiceStatus
{
    Draft = 1,
    Signed = 2,
    Sent = 3,
    Accepted = 4,
    Rejected = 5,
}

/// <summary>
/// Representa un documento tributario electrónico oficial de Costa Rica ante el Ministerio de Hacienda (DGT v4.3/v4.4).
/// </summary>
public sealed class ElectronicInvoice
{
    private ElectronicInvoice() { } // EF Core

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public Guid? TransactionId { get; private set; }
    public ElectronicInvoiceDocumentType DocumentType { get; private set; }
    public ElectronicInvoiceStatus Status { get; private set; }
    public string ClaveNumerica { get; private set; } = string.Empty; // 50 dígitos
    public string NumeroConsecutivo { get; private set; } = string.Empty; // 20 dígitos
    public string CodigoCabys { get; private set; } = string.Empty;
    public string ServiceDescription { get; private set; } = string.Empty;
    public decimal SubtotalCrc { get; private set; }
    public decimal IvaRate { get; private set; } = 0.13m;
    public decimal IvaAmountCrc { get; private set; }
    public decimal TotalAmountCrc { get; private set; }
    public string PaymentMethodCode { get; private set; } = "02"; // 02=Tarjeta, 04=SINPE

    // Datos del Receptor al momento de la emisión
    public TaxIdentificationType? ReceiverIdType { get; private set; }
    public string? ReceiverIdNumber { get; private set; }
    public string ReceiverName { get; private set; } = string.Empty;
    public string ReceiverEmail { get; private set; } = string.Empty;

    // Enlaces a los artefactos requeridos por ley (conservación 5 años)
    public string? SignedXmlUrl { get; private set; }
    public string? HaciendaResponseXmlUrl { get; private set; }
    public string? PdfRepresentationUrl { get; private set; }
    public string? HaciendaResponseStatus { get; private set; }
    public string? HaciendaErrorMessage { get; private set; }

    public DateTimeOffset IssuedAt { get; private set; }
    public DateTimeOffset? ProcessedByHaciendaAt { get; private set; }

    public static ElectronicInvoice Create(
        Guid userId,
        ElectronicInvoiceDocumentType documentType,
        string claveNumerica,
        string numeroConsecutivo,
        string codigoCabys,
        string serviceDescription,
        decimal totalAmountCrc,
        string receiverName,
        string receiverEmail,
        TaxIdentificationType? receiverIdType = null,
        string? receiverIdNumber = null,
        Guid? transactionId = null,
        string paymentMethodCode = "02")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(claveNumerica);
        ArgumentException.ThrowIfNullOrWhiteSpace(numeroConsecutivo);
        ArgumentException.ThrowIfNullOrWhiteSpace(codigoCabys);
        ArgumentException.ThrowIfNullOrWhiteSpace(receiverName);
        ArgumentException.ThrowIfNullOrWhiteSpace(receiverEmail);

        var subtotal = Math.Round(totalAmountCrc / 1.13m, 2);
        var iva = totalAmountCrc - subtotal;

        return new ElectronicInvoice
        {
            Id = Guid.CreateVersion7(),
            UserId = userId,
            TransactionId = transactionId,
            DocumentType = documentType,
            Status = ElectronicInvoiceStatus.Draft,
            ClaveNumerica = claveNumerica.Trim(),
            NumeroConsecutivo = numeroConsecutivo.Trim(),
            CodigoCabys = codigoCabys.Trim(),
            ServiceDescription = serviceDescription.Trim(),
            SubtotalCrc = subtotal,
            IvaRate = CabysCatalog.StandardIvaRate,
            IvaAmountCrc = iva,
            TotalAmountCrc = totalAmountCrc,
            PaymentMethodCode = paymentMethodCode,
            ReceiverName = receiverName.Trim(),
            ReceiverEmail = receiverEmail.Trim().ToLowerInvariant(),
            ReceiverIdType = receiverIdType,
            ReceiverIdNumber = receiverIdNumber?.Trim(),
            IssuedAt = DateTimeOffset.UtcNow,
        };
    }

    public void MarkSigned(string signedXmlUrl)
    {
        SignedXmlUrl = signedXmlUrl;
        Status = ElectronicInvoiceStatus.Signed;
    }

    public void MarkSentToHacienda()
    {
        Status = ElectronicInvoiceStatus.Sent;
    }

    public void MarkAcceptedByHacienda(string responseXmlUrl, string? pdfUrl = null)
    {
        Status = ElectronicInvoiceStatus.Accepted;
        HaciendaResponseStatus = "ACEPTADO";
        HaciendaResponseXmlUrl = responseXmlUrl;
        if (pdfUrl is not null) PdfRepresentationUrl = pdfUrl;
        ProcessedByHaciendaAt = DateTimeOffset.UtcNow;
    }

    public void MarkRejectedByHacienda(string responseXmlUrl, string reason)
    {
        Status = ElectronicInvoiceStatus.Rejected;
        HaciendaResponseStatus = "RECHAZADO";
        HaciendaErrorMessage = reason;
        HaciendaResponseXmlUrl = responseXmlUrl;
        ProcessedByHaciendaAt = DateTimeOffset.UtcNow;
    }

    public void SetPdfUrl(string pdfUrl)
    {
        PdfRepresentationUrl = pdfUrl;
    }
}
