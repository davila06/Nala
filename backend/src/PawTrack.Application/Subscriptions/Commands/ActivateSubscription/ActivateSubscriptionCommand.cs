using MediatR;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Subscriptions.DTOs;
using PawTrack.Application.Subscriptions.Interfaces;
using PawTrack.Domain.Common;
using PawTrack.Domain.Subscriptions;

namespace PawTrack.Application.Subscriptions.Commands.ActivateSubscription;

public sealed record ActivateSubscriptionCommand(string PaymentReference) : IRequest<Result<SubscriptionDto>>;

public sealed class ActivateSubscriptionCommandHandler(
    ISubscriptionRepository subscriptionRepository,
    IClinicRepository clinicRepository,
    IStoreRepository storeRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<ActivateSubscriptionCommand, Result<SubscriptionDto>>
{
    public async Task<Result<SubscriptionDto>> Handle(
        ActivateSubscriptionCommand request,
        CancellationToken cancellationToken)
    {
        var subscription = await subscriptionRepository
            .GetByPaymentReferenceAsync(request.PaymentReference, cancellationToken);

        if (subscription is null)
            return Result.Failure<SubscriptionDto>("Payment reference not found.");

        if (subscription.Status != SubscriptionStatus.PendingPayment)
            return Result.Failure<SubscriptionDto>("Subscription is not in a pending state.");

        subscription.Activate();
        subscriptionRepository.Update(subscription);
        await SyncClinicFeaturedAsync(subscription, true, cancellationToken);
        await SyncStoreFeaturedAsync(subscription, true, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(SubscriptionDto.FromDomain(subscription));
    }

    private async Task SyncClinicFeaturedAsync(
        Domain.Subscriptions.Subscription sub, bool featured, CancellationToken ct)
    {
        if (sub.ClinicId is null) return;
        if (sub.Tier < SubscriptionTier.ClinicPlus) return;

        var clinic = await clinicRepository.GetByIdAsync(sub.ClinicId.Value, ct);
        if (clinic is null) return;
        clinic.SetFeatured(featured);
        clinicRepository.Update(clinic);
    }

    private async Task SyncStoreFeaturedAsync(
        Domain.Subscriptions.Subscription sub, bool featured, CancellationToken ct)
    {
        if (sub.UserId is null) return;
        if (sub.Tier is not (SubscriptionTier.StorePlus or SubscriptionTier.StorePartner)) return;

        var store = await storeRepository.GetByUserIdAsync(sub.UserId.Value, ct);
        if (store is null) return;
        store.SetFeatured(featured);
        storeRepository.Update(store);
    }
}
