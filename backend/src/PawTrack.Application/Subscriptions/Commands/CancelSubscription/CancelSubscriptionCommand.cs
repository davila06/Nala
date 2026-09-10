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

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(SubscriptionDto.FromDomain(subscription));
    }
}
