using PawTrack.Domain.Common;

namespace PawTrack.Application.Clinics.Interfaces;

public interface IClinicFiscalGateway
{
    Task<Result<string>> SubmitAsync(Guid clinicId, Guid saleId, string issuerTaxId, string receiptNumber,
        decimal totalCrc, CancellationToken cancellationToken = default);
}
