using MediatR;
using PawTrack.Application.Subscriptions.DTOs;
using PawTrack.Application.Subscriptions.Interfaces;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Subscriptions.Queries.GetAdminSubscriptionPlans;

public sealed record GetAdminSubscriptionPlansQuery(
    bool IncludeInactive = false,
    int Skip = 0,
    int Take = 50) : IRequest<Result<IReadOnlyList<SubscriptionPlanDto>>>;

public sealed class GetAdminSubscriptionPlansQueryHandler(ISubscriptionPlanRepository repository)
    : IRequestHandler<GetAdminSubscriptionPlansQuery, Result<IReadOnlyList<SubscriptionPlanDto>>>
{
    public async Task<Result<IReadOnlyList<SubscriptionPlanDto>>> Handle(
        GetAdminSubscriptionPlansQuery request,
        CancellationToken cancellationToken)
    {
        var plans = await repository.GetPagedAsync(
            Math.Max(0, request.Skip),
            Math.Clamp(request.Take, 1, 100),
            request.IncludeInactive,
            cancellationToken);
        return Result.Success<IReadOnlyList<SubscriptionPlanDto>>(
            plans.Select(SubscriptionPlanDto.FromDomain).ToList());
    }
}