using PawTrack.Domain.Payments;

namespace PawTrack.Application.Payments.Interfaces;

public interface IPaymentTransactionRepository
{
    Task<PaymentTransaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PaymentTransaction?> GetByReferenceAsync(string reference, CancellationToken cancellationToken = default);
    Task<bool> ExistsByReferenceAsync(string reference, CancellationToken cancellationToken = default);
    Task AddAsync(PaymentTransaction transaction, CancellationToken cancellationToken = default);
    void Update(PaymentTransaction transaction);
}
