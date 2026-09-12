using PawTrack.Application.Payments.DTOs;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Payments.Interfaces;

public interface IElectronicBillingService
{
    Task<Result<ElectronicInvoiceDto>> EmitInvoiceForTransactionAsync(
        EmitInvoiceRequest request,
        CancellationToken cancellationToken = default);

    Task<byte[]> GenerateInvoicePdfAsync(
        Guid invoiceId,
        CancellationToken cancellationToken = default);
}
