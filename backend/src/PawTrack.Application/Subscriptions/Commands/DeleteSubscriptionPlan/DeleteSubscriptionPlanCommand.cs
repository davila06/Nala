using MediatR;
using PawTrack.Application.Subscriptions.DTOs;
using PawTrack.Application.Subscriptions.Interfaces;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Subscriptions.Commands.DeleteSubscriptionPlan;

public sealed record DeleteSubscriptionPlanCommand(Guid Id, Guid Version)
    : IRequest<Result<SubscriptionPlanDto>>;

public sealed class DeleteSubscriptionPlanCommandHandler(ISubscriptionPlanRepository repository)
    : IRequestHandler<DeleteSubscriptionPlanCommand, Result<SubscriptionPlanDto>>
{
    public async Task<Result<SubscriptionPlanDto>> Handle(
        DeleteSubscriptionPlanCommand request,
        CancellationToken cancellationToken)
    {
        var plan = await repository.GetByIdAsync(request.Id, cancellationToken);
        if (plan is null)
            return Result.Failure<SubscriptionPlanDto>("Subscription plan not found.");
        if (plan.Version != request.Version)
            return Result.Failure<SubscriptionPlanDto>("The subscription plan was modified by another administrator.");

        plan.Deactivate();
        repository.Update(plan);
        return Result.Success(SubscriptionPlanDto.FromDomain(plan));
    }
}