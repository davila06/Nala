using Microsoft.EntityFrameworkCore;
using PawTrack.Application.Payments.Interfaces;
using PawTrack.Domain.Payments;
using PawTrack.Infrastructure.Persistence;

namespace PawTrack.Infrastructure.Payments;

public sealed class PaymentLedgerRepository(PawTrackDbContext db) : IPaymentLedgerRepository
{
    public Task AddAsync(PaymentLedgerEntry entry, CancellationToken cancellationToken = default) =>
        db.PaymentLedgerEntries.AddAsync(entry, cancellationToken).AsTask();

    public Task<bool> ExistsByOperationIdAsync(Guid paymentOperationId, CancellationToken cancellationToken = default) =>
        db.PaymentLedgerEntries.AnyAsync(x => x.PaymentOperationId == paymentOperationId, cancellationToken);

    public Task<bool> ExistsByIntentAndEntryTypeAsync(
        Guid paymentIntentId,
        PaymentLedgerEntryType entryType,
        CancellationToken cancellationToken = default) =>
        db.PaymentLedgerEntries.AnyAsync(
            x => x.PaymentIntentId == paymentIntentId && x.EntryType == entryType,
            cancellationToken);
}
