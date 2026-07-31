using MediatR;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Subscriptions.DTOs;
using PawTrack.Application.Subscriptions.Interfaces;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Subscriptions.Commands.ActivateSubscription;

public sealed record ActivateSubscriptionCommand(string PaymentReference) : IRequest<Result<SubscriptionDto>>;

public sealed class ActivateSubscriptionCommandHandler(
    ISubscriptionRepository subscriptionRepository,
    IUnitOfWork             unitOfWork)
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

        if (subscription.Status != Domain.Subscriptions.SubscriptionStatus.PendingPayment)
            return Result.Failure<SubscriptionDto>("Subscription is not in a pending state.");

        subscription.Activate();
        subscriptionRepository.Update(subscription);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(SubscriptionDto.FromDomain(subscription));
    }
}
