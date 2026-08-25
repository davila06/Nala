using MediatR;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Subscriptions.DTOs;
using PawTrack.Application.Subscriptions.Interfaces;
using PawTrack.Domain.Common;
using PawTrack.Domain.Subscriptions;

namespace PawTrack.Application.Subscriptions.Commands.CancelSubscription;

public sealed record CancelSubscriptionCommand(Guid SubscriptionId, Guid RequestingUserId) : IRequest<Result<SubscriptionDto>>;

public sealed class CancelSubscriptionCommandHandler(
    ISubscriptionRepository subscriptionRepository,
    IClinicRepository clinicRepository,
    IStoreRepository storeRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<CancelSubscriptionCommand, Result<SubscriptionDto>>
{
    public async Task<Result<SubscriptionDto>> Handle(
        CancelSubscriptionCommand request,
        CancellationToken cancellationToken)
    {
        var subscription = await subscriptionRepository.GetByIdAsync(request.SubscriptionId, cancellationToken);
        if (subscription is null)
            return Result.Failure<SubscriptionDto>("Subscription not found.");

        // Guid.Empty = admin bypass; otherwise enforce ownership
        if (request.RequestingUserId != Guid.Empty &&
            subscription.UserId != request.RequestingUserId &&
            subscription.ClinicOwnerId != request.RequestingUserId)
            return Result.Failure<SubscriptionDto>("Access denied.");

        subscription.Cancel();
        subscriptionRepository.Update(subscription);

        // Remove featured flag when a clinic downgrade/cancels
        if (subscription.ClinicId.HasValue && subscription.Tier >= SubscriptionTier.ClinicPlus)
        {
            var clinic = await clinicRepository.GetByIdAsync(subscription.ClinicId.Value, cancellationToken);
            if (clinic is not null)
            {
                clinic.SetFeatured(false);
                clinicRepository.Update(clinic);
            }
        }

        // Remove featured flag when a store downgrades/cancels
        if (subscription.UserId.HasValue &&
            subscription.Tier is SubscriptionTier.StorePlus or SubscriptionTier.StorePartner)
        {
            var store = await storeRepository.GetByUserIdAsync(subscription.UserId.Value, cancellationToken);
            if (store is not null)
            {
                store.SetFeatured(false);
                storeRepository.Update(store);
            }
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(SubscriptionDto.FromDomain(subscription));
    }
}
