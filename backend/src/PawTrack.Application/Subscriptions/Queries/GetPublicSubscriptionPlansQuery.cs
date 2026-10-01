using MediatR;
using PawTrack.Application.Subscriptions.DTOs;
using PawTrack.Application.Subscriptions.Interfaces;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Subscriptions.Queries;

public sealed record GetPublicSubscriptionPlansQuery
    : IRequest<Result<IReadOnlyList<PublicSubscriptionPlanDto>>>;

public sealed class GetPublicSubscriptionPlansQueryHandler(ISubscriptionPlanRepository repository)
    : IRequestHandler<GetPublicSubscriptionPlansQuery, Result<IReadOnlyList<PublicSubscriptionPlanDto>>>
{
    public async Task<Result<IReadOnlyList<PublicSubscriptionPlanDto>>> Handle(
        GetPublicSubscriptionPlansQuery request,
        CancellationToken cancellationToken)
    {
        var plans = await repository.GetCommerciallyApprovedPagedAsync(0, 100, cancellationToken);
        return Result.Success<IReadOnlyList<PublicSubscriptionPlanDto>>(
            plans.Select(PublicSubscriptionPlanDto.FromDomain).ToList());
    }
}
