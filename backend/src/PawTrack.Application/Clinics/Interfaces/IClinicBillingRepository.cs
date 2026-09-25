using PawTrack.Domain.Clinics;

namespace PawTrack.Application.Clinics.Interfaces;

public interface IClinicBillingRepository
{
    Task<ClinicSale?> GetSaleByIdAsync(Guid saleId, CancellationToken cancellationToken = default);
    Task<ClinicFiscalSubmission?> GetFiscalSubmissionAsync(Guid saleId, CancellationToken cancellationToken = default);
    Task<bool> HasCashCloseAsync(Guid clinicId, DateOnly businessDate, CancellationToken cancellationToken = default);
    Task AddFiscalSubmissionAsync(ClinicFiscalSubmission submission, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ClinicSale>> GetSalesForClinicOnDateAsync(Guid clinicId, DateOnly businessDate, CancellationToken cancellationToken = default);
    Task<IReadOnlyDictionary<string, decimal>> GetSalesByVeterinarianForClinicOnDateAsync(Guid clinicId, DateOnly businessDate, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ClinicSalePayment>> GetPaymentsForClinicOnDateAsync(Guid clinicId, DateOnly businessDate, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ClinicSaleRefund>> GetRefundsForClinicOnDateAsync(Guid clinicId, DateOnly businessDate, CancellationToken cancellationToken = default);
    Task AddSaleAsync(ClinicSale sale, CancellationToken cancellationToken = default);
    void UpdateSale(ClinicSale sale);
    Task AddCashCloseAsync(ClinicCashClose cashClose, CancellationToken cancellationToken = default);
}
