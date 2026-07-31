using MediatR;
using PawTrack.Application.Subscriptions.DTOs;
using PawTrack.Application.Subscriptions.Interfaces;
using PawTrack.Domain.Common;
using PawTrack.Domain.Subscriptions;

namespace PawTrack.Application.Subscriptions.Queries.GetMySubscription;

public sealed record GetMySubscriptionQuery(Guid? UserId, Guid? ClinicId) : IRequest<Result<SubscriptionDto?>>;

public sealed class GetMySubscriptionQueryHandler(ISubscriptionRepository subscriptionRepository)
    : IRequestHandler<GetMySubscriptionQuery, Result<SubscriptionDto?>>
{
    public async Task<Result<SubscriptionDto?>> Handle(
        GetMySubscriptionQuery request,
        CancellationToken cancellationToken)
    {
        Subscription? sub = request.UserId.HasValue
            ? await subscriptionRepository.GetActiveForUserAsync(request.UserId.Value, cancellationToken)
            : request.ClinicId.HasValue
                ? await subscriptionRepository.GetActiveForClinicAsync(request.ClinicId.Value, cancellationToken)
                : null;

        return Result.Success<SubscriptionDto?>(sub is null ? null : SubscriptionDto.FromDomain(sub));
    }
}
