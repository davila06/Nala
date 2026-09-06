using MediatR;
using PawTrack.Application.Subscriptions.DTOs;
using PawTrack.Application.Subscriptions.Interfaces;
using PawTrack.Domain.Common;
using PawTrack.Domain.Subscriptions;

namespace PawTrack.Application.Subscriptions.Commands.CreateSubscriptionPlan;

public sealed record CreateSubscriptionPlanCommand(
    SubscriptionTier Tier,
    string DisplayName,
    string Description,
    decimal? MonthlyPriceCrc,
    decimal? AnnualPriceCrc) : IRequest<Result<SubscriptionPlanDto>>;

public sealed class CreateSubscriptionPlanCommandHandler(ISubscriptionPlanRepository repository)
    : IRequestHandler<CreateSubscriptionPlanCommand, Result<SubscriptionPlanDto>>
{
    public async Task<Result<SubscriptionPlanDto>> Handle(
        CreateSubscriptionPlanCommand request,
        CancellationToken cancellationToken)
    {
        if (await repository.GetByTierAsync(request.Tier, cancellationToken) is not null)
            return Result.Failure<SubscriptionPlanDto>("A plan already exists for this tier.");

        try
        {
            var plan = SubscriptionPlan.Create(
                request.Tier,
                request.DisplayName,
                request.Description,
                request.MonthlyPriceCrc,
                request.AnnualPriceCrc);
            await repository.AddAsync(plan, cancellationToken);
            return Result.Success(SubscriptionPlanDto.FromDomain(plan));
        }
        catch (ArgumentException exception)
        {
            return Result.Failure<SubscriptionPlanDto>(exception.Message);
        }
    }
}