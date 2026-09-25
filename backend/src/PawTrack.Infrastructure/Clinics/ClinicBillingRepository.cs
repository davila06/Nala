using Microsoft.EntityFrameworkCore;
using PawTrack.Application.Clinics.Interfaces;
using PawTrack.Domain.Clinics;
using PawTrack.Infrastructure.Persistence;

namespace PawTrack.Infrastructure.Clinics;

public sealed class ClinicBillingRepository(PawTrackDbContext dbContext) : IClinicBillingRepository
{
    public Task<ClinicFiscalSubmission?> GetFiscalSubmissionAsync(Guid saleId, CancellationToken cancellationToken = default) =>
        dbContext.ClinicFiscalSubmissions.AsNoTracking().FirstOrDefaultAsync(x => x.SaleId == saleId, cancellationToken);

    public Task<bool> HasCashCloseAsync(Guid clinicId, DateOnly businessDate, CancellationToken cancellationToken = default) =>
        dbContext.ClinicCashCloses.AsNoTracking().AnyAsync(x => x.ClinicId == clinicId && x.BusinessDate == businessDate, cancellationToken);

    public async Task AddFiscalSubmissionAsync(ClinicFiscalSubmission submission, CancellationToken cancellationToken = default) =>
        await dbContext.ClinicFiscalSubmissions.AddAsync(submission, cancellationToken);

    public Task<ClinicSale?> GetSaleByIdAsync(Guid saleId, CancellationToken cancellationToken = default) =>
        dbContext.ClinicSales
            .Include(sale => sale.Lines)
            .Include(sale => sale.Payments)
            .Include(sale => sale.Refunds)
            .FirstOrDefaultAsync(sale => sale.Id == saleId, cancellationToken);

    public async Task<IReadOnlyList<ClinicSale>> GetSalesForClinicOnDateAsync(Guid clinicId, DateOnly businessDate, CancellationToken cancellationToken = default)
    {
        var (start, end) = ClinicBusinessDate.UtcRange(businessDate);
        return await dbContext.ClinicSales.AsNoTracking()
            .Include(sale => sale.Lines)
            .Include(sale => sale.Payments)
            .Include(sale => sale.Refunds)
            .Where(sale => sale.ClinicId == clinicId && sale.CreatedAt >= start && sale.CreatedAt < end)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyDictionary<string, decimal>> GetSalesByVeterinarianForClinicOnDateAsync(Guid clinicId, DateOnly businessDate, CancellationToken cancellationToken = default)
    {
        var (start, end) = ClinicBusinessDate.UtcRange(businessDate);
        var movements = dbContext.ClinicSalePayments.AsNoTracking()
            .Where(payment => payment.ClinicId == clinicId && payment.ReceivedAt >= start && payment.ReceivedAt < end)
            .Select(payment => new { payment.SaleId, AmountCrc = payment.AmountCrc })
            .Concat(dbContext.ClinicSaleRefunds.AsNoTracking()
                .Where(refund => refund.ClinicId == clinicId && refund.RefundedAt >= start && refund.RefundedAt < end)
                .Select(refund => new { refund.SaleId, AmountCrc = -refund.AmountCrc }));
        var amounts = await (
            from movement in movements
            join sale in dbContext.ClinicSales.AsNoTracking() on movement.SaleId equals sale.Id
            join appointment in dbContext.VeterinarianAppointments.AsNoTracking() on sale.AppointmentId equals appointment.Id
            join veterinarian in dbContext.ClinicVeterinarians.AsNoTracking() on appointment.VeterinarianId equals veterinarian.Id
            where sale.ClinicId == clinicId && appointment.ClinicId == clinicId && veterinarian.ClinicId == clinicId
            group movement by new { veterinarian.Id, veterinarian.FullName, veterinarian.LicenseNumber } into groupByVet
            select new { groupByVet.Key.FullName, groupByVet.Key.LicenseNumber, Amount = groupByVet.Sum(movement => movement.AmountCrc) })
            .ToListAsync(cancellationToken);
        return amounts.ToDictionary(row => $"{row.FullName} ({row.LicenseNumber})", row => row.Amount);
    }

    public async Task<IReadOnlyList<ClinicSalePayment>> GetPaymentsForClinicOnDateAsync(Guid clinicId, DateOnly businessDate, CancellationToken cancellationToken = default)
    {
        var (start, end) = ClinicBusinessDate.UtcRange(businessDate);
        return await dbContext.ClinicSalePayments.AsNoTracking()
            .Where(payment => payment.ClinicId == clinicId && payment.ReceivedAt >= start && payment.ReceivedAt < end)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ClinicSaleRefund>> GetRefundsForClinicOnDateAsync(Guid clinicId, DateOnly businessDate, CancellationToken cancellationToken = default)
    {
        var (start, end) = ClinicBusinessDate.UtcRange(businessDate);
        return await dbContext.ClinicSaleRefunds.AsNoTracking()
            .Where(refund => refund.ClinicId == clinicId && refund.RefundedAt >= start && refund.RefundedAt < end)
            .ToListAsync(cancellationToken);
    }

    public async Task AddSaleAsync(ClinicSale sale, CancellationToken cancellationToken = default) =>
        await dbContext.ClinicSales.AddAsync(sale, cancellationToken);

    public void UpdateSale(ClinicSale sale) => dbContext.ClinicSales.Update(sale);

    public async Task AddCashCloseAsync(ClinicCashClose cashClose, CancellationToken cancellationToken = default) =>
        await dbContext.ClinicCashCloses.AddAsync(cashClose, cancellationToken);
}
