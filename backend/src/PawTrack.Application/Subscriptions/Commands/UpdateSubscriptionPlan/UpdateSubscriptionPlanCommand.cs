using MediatR;
using PawTrack.Application.Subscriptions.DTOs;
using PawTrack.Application.Subscriptions.Interfaces;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Subscriptions.Commands.UpdateSubscriptionPlan;

public sealed record UpdateSubscriptionPlanCommand(
    Guid Id,
    Guid Version,
    string DisplayName,
    string Description,
    decimal? MonthlyPriceCrc,
    decimal? AnnualPriceCrc) : IRequest<Result<SubscriptionPlanDto>>;

public sealed class UpdateSubscriptionPlanCommandHandler(ISubscriptionPlanRepository repository)
    : IRequestHandler<UpdateSubscriptionPlanCommand, Result<SubscriptionPlanDto>>
{
    public async Task<Result<SubscriptionPlanDto>> Handle(
        UpdateSubscriptionPlanCommand request,
        CancellationToken cancellationToken)
    {
        var plan = await repository.GetByIdAsync(request.Id, cancellationToken);
        if (plan is null)
            return Result.Failure<SubscriptionPlanDto>("Subscription plan not found.");
        if (plan.Version != request.Version)
            return Result.Failure<SubscriptionPlanDto>("The subscription plan was modified by another administrator.");

        try
        {
            plan.Update(request.DisplayName, request.Description, request.MonthlyPriceCrc, request.AnnualPriceCrc);
            repository.Update(plan);
            return Result.Success(SubscriptionPlanDto.FromDomain(plan));
        }
        catch (ArgumentException exception)
        {
            return Result.Failure<SubscriptionPlanDto>(exception.Message);
        }
    }
}