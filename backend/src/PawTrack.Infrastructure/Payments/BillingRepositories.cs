using Microsoft.EntityFrameworkCore;
using PawTrack.Application.Payments.Interfaces;
using PawTrack.Domain.Payments;
using PawTrack.Infrastructure.Persistence;

namespace PawTrack.Infrastructure.Payments;

public sealed class UserBillingProfileRepository(PawTrackDbContext db) : IUserBillingProfileRepository
{
    public async Task<UserBillingProfile?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
        await db.UserBillingProfiles.FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);

    public async Task AddAsync(UserBillingProfile profile, CancellationToken cancellationToken = default) =>
        await db.UserBillingProfiles.AddAsync(profile, cancellationToken);

    public void Update(UserBillingProfile profile) =>
        db.UserBillingProfiles.Update(profile);
}

public sealed class ElectronicInvoiceRepository(PawTrackDbContext db) : IElectronicInvoiceRepository
{
    public async Task<ElectronicInvoice?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await db.ElectronicInvoices.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<ElectronicInvoice?> GetByClaveAsync(string clave, CancellationToken cancellationToken = default) =>
        await db.ElectronicInvoices.FirstOrDefaultAsync(x => x.ClaveNumerica == clave, cancellationToken);

    public async Task<IReadOnlyList<ElectronicInvoice>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
        await db.ElectronicInvoices
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.IssuedAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ElectronicInvoice>> GetByPeriodAsync(
        DateTimeOffset startDate,
        DateTimeOffset endDate,
        CancellationToken cancellationToken = default) =>
        await db.ElectronicInvoices
            .Where(x => x.IssuedAt >= startDate && x.IssuedAt < endDate && x.Status != ElectronicInvoiceStatus.Rejected)
            .OrderBy(x => x.IssuedAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ElectronicInvoice>> GetPendingHaciendaAsync(CancellationToken cancellationToken = default) =>
        await db.ElectronicInvoices
            .Where(x => x.Status == ElectronicInvoiceStatus.Sent || x.Status == ElectronicInvoiceStatus.Signed)
            .OrderBy(x => x.IssuedAt)
            .ToListAsync(cancellationToken);

    public async Task<string> GetNextSequenceNumberAsync(
        ElectronicInvoiceDocumentType documentType,
        CancellationToken cancellationToken = default)
    {
        var count = await db.ElectronicInvoices.CountAsync(x => x.DocumentType == documentType, cancellationToken);
        var nextVal = count + 1;
        return nextVal.ToString().PadLeft(10, '0');
    }

    public async Task AddAsync(ElectronicInvoice invoice, CancellationToken cancellationToken = default) =>
        await db.ElectronicInvoices.AddAsync(invoice, cancellationToken);

    public void Update(ElectronicInvoice invoice) =>
        db.ElectronicInvoices.Update(invoice);
}
