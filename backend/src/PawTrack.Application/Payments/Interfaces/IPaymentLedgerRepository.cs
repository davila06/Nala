using PawTrack.Domain.Payments;

namespace PawTrack.Application.Payments.Interfaces;

public interface IPaymentLedgerRepository
{
    Task AddAsync(PaymentLedgerEntry entry, CancellationToken cancellationToken = default);
    Task<bool> ExistsByOperationIdAsync(Guid paymentOperationId, CancellationToken cancellationToken = default);
    Task<bool> ExistsByIntentAndEntryTypeAsync(
        Guid paymentIntentId,
        PaymentLedgerEntryType entryType,
        CancellationToken cancellationToken = default);
}
