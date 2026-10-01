using PawTrack.Domain.Payments;

namespace PawTrack.Application.Payments.Interfaces;

public interface IPaymentIntentRepository
{
    Task<PaymentIntent?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PaymentIntent?> GetByIdempotencyKeyAsync(Guid userId, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<PaymentIntent?> GetByMerchantReferenceAsync(string merchantReference, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PaymentIntent>> GetByStatusSinceAsync(
        PaymentIntentStatus status,
        DateTimeOffset since,
        CancellationToken cancellationToken = default);
    Task AddAsync(PaymentIntent intent, CancellationToken cancellationToken = default);
    void Update(PaymentIntent intent);
}

public interface IPaymentOperationRepository
{
    Task<PaymentOperation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PaymentOperation?> GetByIdempotencyKeyAsync(
        PaymentOperationType operationType,
        string idempotencyKey,
        CancellationToken cancellationToken = default);
    Task<PaymentOperation?> GetByProviderOperationIdAsync(
        PaymentOperationType operationType,
        string providerOperationId,
        CancellationToken cancellationToken = default);
    Task AddAsync(PaymentOperation operation, CancellationToken cancellationToken = default);
    void Update(PaymentOperation operation);
}
