using Microsoft.EntityFrameworkCore;
using PawTrack.Application.Payments.Interfaces;
using PawTrack.Domain.Payments;
using PawTrack.Infrastructure.Persistence;

namespace PawTrack.Infrastructure.Payments;

public sealed class PaymentIntentRepository(PawTrackDbContext db) : IPaymentIntentRepository
{
    public Task<PaymentIntent?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.PaymentIntents.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<PaymentIntent?> GetByIdempotencyKeyAsync(
        Guid userId,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        db.PaymentIntents.FirstOrDefaultAsync(
            x => x.UserId == userId && x.IdempotencyKey == idempotencyKey,
            cancellationToken);

    public Task<PaymentIntent?> GetByMerchantReferenceAsync(
        string merchantReference,
        CancellationToken cancellationToken = default) =>
        db.PaymentIntents.FirstOrDefaultAsync(
            x => x.MerchantReference == merchantReference,
            cancellationToken);

    public Task AddAsync(PaymentIntent intent, CancellationToken cancellationToken = default) =>
        db.PaymentIntents.AddAsync(intent, cancellationToken).AsTask();

    public void Update(PaymentIntent intent) => db.PaymentIntents.Update(intent);
}
