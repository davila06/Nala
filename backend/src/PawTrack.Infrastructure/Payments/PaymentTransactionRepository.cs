using Microsoft.EntityFrameworkCore;
using PawTrack.Application.Payments.Interfaces;
using PawTrack.Domain.Payments;
using PawTrack.Infrastructure.Persistence;

namespace PawTrack.Infrastructure.Payments;

public sealed class PaymentTransactionRepository(PawTrackDbContext db) : IPaymentTransactionRepository
{
    public async Task<PaymentTransaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await db.PaymentTransactions.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<PaymentTransaction?> GetByReferenceAsync(string reference, CancellationToken cancellationToken = default) =>
        await db.PaymentTransactions.FirstOrDefaultAsync(x => x.TransactionReference == reference, cancellationToken);

    public async Task<bool> ExistsByReferenceAsync(string reference, CancellationToken cancellationToken = default) =>
        await db.PaymentTransactions.AnyAsync(x => x.TransactionReference == reference, cancellationToken);

    public async Task AddAsync(PaymentTransaction transaction, CancellationToken cancellationToken = default) =>
        await db.PaymentTransactions.AddAsync(transaction, cancellationToken);

    public void Update(PaymentTransaction transaction) => db.PaymentTransactions.Update(transaction);
}
