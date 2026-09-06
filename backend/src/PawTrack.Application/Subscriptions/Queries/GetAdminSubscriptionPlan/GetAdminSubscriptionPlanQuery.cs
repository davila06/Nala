using MediatR;
using PawTrack.Application.Subscriptions.DTOs;
using PawTrack.Application.Subscriptions.Interfaces;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Subscriptions.Queries.GetAdminSubscriptionPlan;

public sealed record GetAdminSubscriptionPlanQuery(Guid Id)
    : IRequest<Result<SubscriptionPlanDto>>;

public sealed class GetAdminSubscriptionPlanQueryHandler(ISubscriptionPlanRepository repository)
    : IRequestHandler<GetAdminSubscriptionPlanQuery, Result<SubscriptionPlanDto>>
{
    public async Task<Result<SubscriptionPlanDto>> Handle(
        GetAdminSubscriptionPlanQuery request,
        CancellationToken cancellationToken)
    {
        var plan = await repository.GetByIdAsync(request.Id, cancellationToken);
        return plan is null
            ? Result.Failure<SubscriptionPlanDto>("Subscription plan not found.")
            : Result.Success(SubscriptionPlanDto.FromDomain(plan));
    }
}