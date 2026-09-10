using MediatR;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Subscriptions.DTOs;
using PawTrack.Application.Subscriptions.Interfaces;
using PawTrack.Domain.Common;
using PawTrack.Domain.Subscriptions;

namespace PawTrack.Application.Subscriptions.Commands.ScheduleSubscriptionDowngrade;

public sealed record ScheduleSubscriptionDowngradeCommand(
    Guid SubscriptionId,
    Guid RequestingUserId,
    SubscriptionTier TargetTier) : IRequest<Result<SubscriptionDto>>;

public sealed class ScheduleSubscriptionDowngradeCommandHandler(
    ISubscriptionRepository subscriptionRepository,
    ISubscriptionPlanRepository planRepository,
    IPaymentService paymentService,
    IUnitOfWork unitOfWork)
    : IRequestHandler<ScheduleSubscriptionDowngradeCommand, Result<SubscriptionDto>>
{
    public async Task<Result<SubscriptionDto>> Handle(
        ScheduleSubscriptionDowngradeCommand request,
        CancellationToken cancellationToken)
    {
        var current = await subscriptionRepository.GetByIdAsync(request.SubscriptionId, cancellationToken);
        if (current is null)
            return Result.Failure<SubscriptionDto>("Subscription not found.");

        if (current.UserId != request.RequestingUserId)
            return Result.Failure<SubscriptionDto>("Access denied.");

        if (!current.IsActive)
            return Result.Failure<SubscriptionDto>("Only an active subscription can be downgraded.");

        if (current.Tier != SubscriptionTier.UserFamilia || request.TargetTier != SubscriptionTier.UserPlus)
            return Result.Failure<SubscriptionDto>("Only Familia to Plus downgrade is supported.");

        if (current.ExpiresAt is null)
            return Result.Failure<SubscriptionDto>("The current subscription has no expiry date.");

        var existingPending = await subscriptionRepository.GetPendingForUserAsync(
            request.RequestingUserId, cancellationToken);
        if (existingPending is not null)
            return Result.Failure<SubscriptionDto>("A pending plan change already exists.");

        var plan = await planRepository.GetByTierAsync(request.TargetTier, cancellationToken);
        if (plan is null || !plan.IsActive || plan.MonthlyPriceCrc is null)
            return Result.Failure<SubscriptionDto>("The target plan is not available.");

        current.Cancel();
        subscriptionRepository.Update(current);

        var scheduled = Subscription.CreateScheduledForUser(
            request.RequestingUserId,
            request.TargetTier,
            paymentService.GenerateReference(),
            plan.MonthlyPriceCrc.Value,
            current.ExpiresAt.Value);

        await subscriptionRepository.AddAsync(scheduled, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(SubscriptionDto.FromDomain(scheduled));
    }
}
