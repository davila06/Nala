using PawTrack.Domain.Payments;

namespace PawTrack.Application.Payments.DTOs;

public sealed record UserBillingProfileDto(
    Guid Id,
    Guid UserId,
    string IdentificationType,
    string IdentificationNumber,
    string LegalName,
    string BillingEmail,
    bool RequiresInvoice,
    string? Province,
    string? Canton,
    string? District,
    string? AddressDetails,
    string? PhoneNumber,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt)
{
    public static UserBillingProfileDto FromDomain(UserBillingProfile p) => new(
        p.Id,
        p.UserId,
        p.IdentificationType.ToString(),
        p.IdentificationNumber,
        p.LegalName,
        p.BillingEmail,
        p.RequiresInvoice,
        p.Province,
        p.Canton,
        p.District,
        p.AddressDetails,
        p.PhoneNumber,
        p.CreatedAt,
        p.UpdatedAt);
}

public sealed record ElectronicInvoiceDto(
    Guid Id,
    Guid UserId,
    string DocumentType,
    string Status,
    string ClaveNumerica,
    string NumeroConsecutivo,
    string CodigoCabys,
    string ServiceDescription,
    decimal SubtotalCrc,
    decimal IvaAmountCrc,
    decimal TotalAmountCrc,
    string PaymentMethodCode,
    string ReceiverName,
    string ReceiverEmail,
    string? ReceiverIdNumber,
    string? PdfUrl,
    string? XmlUrl,
    DateTimeOffset IssuedAt,
    DateTimeOffset? ProcessedAt)
{
    public static ElectronicInvoiceDto FromDomain(ElectronicInvoice i) => new(
        i.Id,
        i.UserId,
        i.DocumentType.ToString(),
        i.Status.ToString(),
        i.ClaveNumerica,
        i.NumeroConsecutivo,
        i.CodigoCabys,
        i.ServiceDescription,
        i.SubtotalCrc,
        i.IvaAmountCrc,
        i.TotalAmountCrc,
        i.PaymentMethodCode,
        i.ReceiverName,
        i.ReceiverEmail,
        i.ReceiverIdNumber,
        i.PdfRepresentationUrl,
        i.SignedXmlUrl,
        i.IssuedAt,
        i.ProcessedByHaciendaAt);
}

public sealed record EmitInvoiceRequest(
    Guid UserId,
    decimal TotalAmountCrc,
    string Description,
    string CodigoCabys,
    string PaymentMethodCode = "02", // 02=Tarjeta, 04=SINPE
    Guid? TransactionId = null,
    bool ForceInvoice = false);
