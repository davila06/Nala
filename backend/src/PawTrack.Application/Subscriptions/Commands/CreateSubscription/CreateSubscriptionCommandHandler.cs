using MediatR;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Payments.Interfaces;
using PawTrack.Application.Subscriptions.DTOs;
using PawTrack.Application.Subscriptions.Interfaces;
using PawTrack.Domain.Common;
using PawTrack.Domain.Subscriptions;

namespace PawTrack.Application.Subscriptions.Commands.CreateSubscription;

public sealed class CreateSubscriptionCommandHandler(
    ISubscriptionRepository subscriptionRepository,
    ISubscriptionPlanRepository planRepository,
    IPaymentService paymentService,
    IUserBillingProfileRepository? billingProfileRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<CreateSubscriptionCommand, Result<SubscriptionDto>>
{
    public CreateSubscriptionCommandHandler(
        ISubscriptionRepository subscriptionRepository,
        ISubscriptionPlanRepository planRepository,
        IPaymentService paymentService,
        IUnitOfWork unitOfWork)
        : this(subscriptionRepository, planRepository, paymentService, null, unitOfWork) { }

    public async Task<Result<SubscriptionDto>> Handle(
        CreateSubscriptionCommand request,
        CancellationToken cancellationToken)
    {
        var plan = await planRepository.GetByTierAsync(request.Tier, cancellationToken);
        if (plan is null || !plan.IsActive)
            return Result.Failure<SubscriptionDto>($"Tier {request.Tier} is not an active paid plan.");

        if (SubscriptionPricing.IsUserTermTier(request.Tier) && plan.MonthlyPriceCrc is null)
            return Result.Failure<SubscriptionDto>("This user plan has no monthly price configured.");

        var billingMonths = SubscriptionPricing.IsUserTermTier(request.Tier)
            ? request.BillingMonths
            : SubscriptionPricing.IsMunicipalTier(request.Tier) ? 12 : 1;
        var baseAmount = SubscriptionPricing.IsUserTermTier(request.Tier)
            ? SubscriptionPricing.CalculateTermPriceCrc(plan.MonthlyPriceCrc!.Value, billingMonths)
            : plan.AnnualPriceCrc ?? plan.MonthlyPriceCrc!.Value;

        var requiresInvoice = request.RequiresInvoice ?? false;
        if (!request.RequiresInvoice.HasValue && billingProfileRepository is not null)
        {
            var targetUserId = request.UserId ?? request.RequestingUserId;
            var profile = await billingProfileRepository.GetByUserIdAsync(targetUserId, cancellationToken);
            if (profile is not null && profile.RequiresInvoice)
            {
                requiresInvoice = true;
            }
        }

        var amount = SubscriptionPricing.GetEffectivePriceCrc(baseAmount, requiresInvoice);

        // Cancel any existing pending subscription for the same owner before creating a new one
        Subscription? existing = request.UserId.HasValue
            ? await subscriptionRepository.GetActiveForUserAsync(request.UserId.Value, cancellationToken)
            : request.ClinicId.HasValue
                ? await subscriptionRepository.GetActiveForClinicAsync(request.ClinicId.Value, cancellationToken)
                : null;

        if (existing is not null && existing.IsActive)
            return Result.Failure<SubscriptionDto>("An active subscription already exists. Cancel it before upgrading.");

        var reference = paymentService.GenerateReference();
        var subscription = request.UserId.HasValue
            ? Subscription.CreateForUser(request.UserId.Value, request.Tier, reference, amount, billingMonths)
            : Subscription.CreateForClinic(
                request.ClinicId!.Value,
                request.RequestingUserId,
                request.Tier,
                reference,
                amount,
                billingMonths);

        await subscriptionRepository.AddAsync(subscription, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(SubscriptionDto.FromDomain(subscription));
    }
}
