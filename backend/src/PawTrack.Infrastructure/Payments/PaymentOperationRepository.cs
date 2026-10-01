using Microsoft.EntityFrameworkCore;
using PawTrack.Application.Payments.Interfaces;
using PawTrack.Domain.Payments;
using PawTrack.Infrastructure.Persistence;

namespace PawTrack.Infrastructure.Payments;

public sealed class PaymentOperationRepository(PawTrackDbContext db) : IPaymentOperationRepository
{
    public Task<PaymentOperation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.PaymentOperations.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<PaymentOperation?> GetByIdempotencyKeyAsync(
        PaymentOperationType operationType,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        db.PaymentOperations.FirstOrDefaultAsync(
            x => x.OperationType == operationType && x.IdempotencyKey == idempotencyKey,
            cancellationToken);

    public Task<PaymentOperation?> GetByProviderOperationIdAsync(
        PaymentOperationType operationType,
        string providerOperationId,
        CancellationToken cancellationToken = default) =>
        db.PaymentOperations.FirstOrDefaultAsync(
            x => x.OperationType == operationType && x.ProviderOperationId == providerOperationId,
            cancellationToken);

    public Task AddAsync(PaymentOperation operation, CancellationToken cancellationToken = default) =>
        db.PaymentOperations.AddAsync(operation, cancellationToken).AsTask();

    public void Update(PaymentOperation operation) => db.PaymentOperations.Update(operation);
}
