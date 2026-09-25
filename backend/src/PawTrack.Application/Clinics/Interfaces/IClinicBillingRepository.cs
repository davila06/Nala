using PawTrack.Domain.Clinics;

namespace PawTrack.Application.Clinics.Interfaces;

public interface IClinicBillingRepository
{
    Task<ClinicSale?> GetSaleByIdAsync(Guid saleId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ClinicSale>> GetSalesForClinicOnDateAsync(Guid clinicId, DateOnly businessDate, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ClinicSalePayment>> GetPaymentsForClinicOnDateAsync(Guid clinicId, DateOnly businessDate, CancellationToken cancellationToken = default);
    Task AddSaleAsync(ClinicSale sale, CancellationToken cancellationToken = default);
    void UpdateSale(ClinicSale sale);
    Task AddCashCloseAsync(ClinicCashClose cashClose, CancellationToken cancellationToken = default);
}
