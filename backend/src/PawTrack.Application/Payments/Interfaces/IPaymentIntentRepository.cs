using PawTrack.Domain.Payments;

namespace PawTrack.Application.Payments.Interfaces;

public interface IPaymentIntentRepository
{
    Task<PaymentIntent?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PaymentIntent?> GetByIdempotencyKeyAsync(Guid userId, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<PaymentIntent?> GetByMerchantReferenceAsync(string merchantReference, CancellationToken cancellationToken = default);
    Task AddAsync(PaymentIntent intent, CancellationToken cancellationToken = default);
    void Update(PaymentIntent intent);
}
