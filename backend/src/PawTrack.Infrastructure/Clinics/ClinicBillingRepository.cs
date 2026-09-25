using Microsoft.EntityFrameworkCore;
using PawTrack.Application.Clinics.Interfaces;
using PawTrack.Domain.Clinics;
using PawTrack.Infrastructure.Persistence;

namespace PawTrack.Infrastructure.Clinics;

public sealed class ClinicBillingRepository(PawTrackDbContext dbContext) : IClinicBillingRepository
{
    public Task<ClinicSale?> GetSaleByIdAsync(Guid saleId, CancellationToken cancellationToken = default) =>
        dbContext.ClinicSales
            .Include(sale => sale.Lines)
            .Include(sale => sale.Payments)
            .FirstOrDefaultAsync(sale => sale.Id == saleId, cancellationToken);

    public async Task<IReadOnlyList<ClinicSale>> GetSalesForClinicOnDateAsync(Guid clinicId, DateOnly businessDate, CancellationToken cancellationToken = default)
    {
        var start = businessDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var end = start.AddDays(1);
        return await dbContext.ClinicSales.AsNoTracking()
            .Include(sale => sale.Lines)
            .Include(sale => sale.Payments)
            .Where(sale => sale.ClinicId == clinicId && sale.CreatedAt >= start && sale.CreatedAt < end)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ClinicSalePayment>> GetPaymentsForClinicOnDateAsync(Guid clinicId, DateOnly businessDate, CancellationToken cancellationToken = default)
    {
        var start = businessDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var end = start.AddDays(1);
        return await dbContext.ClinicSalePayments.AsNoTracking()
            .Where(payment => payment.ClinicId == clinicId && payment.ReceivedAt >= start && payment.ReceivedAt < end)
            .ToListAsync(cancellationToken);
    }

    public async Task AddSaleAsync(ClinicSale sale, CancellationToken cancellationToken = default) =>
        await dbContext.ClinicSales.AddAsync(sale, cancellationToken);

    public void UpdateSale(ClinicSale sale) => dbContext.ClinicSales.Update(sale);

    public async Task AddCashCloseAsync(ClinicCashClose cashClose, CancellationToken cancellationToken = default) =>
        await dbContext.ClinicCashCloses.AddAsync(cashClose, cancellationToken);
}
