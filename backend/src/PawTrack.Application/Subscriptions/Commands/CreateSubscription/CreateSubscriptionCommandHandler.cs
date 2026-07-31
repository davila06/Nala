using MediatR;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Subscriptions.DTOs;
using PawTrack.Application.Subscriptions.Interfaces;
using PawTrack.Domain.Common;
using PawTrack.Domain.Subscriptions;

namespace PawTrack.Application.Subscriptions.Commands.CreateSubscription;

public sealed class CreateSubscriptionCommandHandler(
    ISubscriptionRepository subscriptionRepository,
    IPaymentService         paymentService,
    IUnitOfWork             unitOfWork)
    : IRequestHandler<CreateSubscriptionCommand, Result<SubscriptionDto>>
{
    // Prices in CRC
    private static readonly Dictionary<SubscriptionTier, decimal> Prices = new()
    {
        [SubscriptionTier.UserPlus]      = 2_990m,
        [SubscriptionTier.UserFamilia]   = 4_990m,
        [SubscriptionTier.ClinicPlus]    = 15_000m,
        [SubscriptionTier.ClinicPartner] = 35_000m,
    };

    public async Task<Result<SubscriptionDto>> Handle(
        CreateSubscriptionCommand request,
        CancellationToken cancellationToken)
    {
        if (!Prices.TryGetValue(request.Tier, out var amount))
            return Result.Failure<SubscriptionDto>($"Tier {request.Tier} is not a paid tier.");

        // Cancel any existing pending subscription for the same owner before creating a new one
        Subscription? existing = request.UserId.HasValue
            ? await subscriptionRepository.GetActiveForUserAsync(request.UserId.Value, cancellationToken)
            : request.ClinicId.HasValue
                ? await subscriptionRepository.GetActiveForClinicAsync(request.ClinicId.Value, cancellationToken)
                : null;

        if (existing is not null && existing.IsActive)
            return Result.Failure<SubscriptionDto>("An active subscription already exists. Cancel it before upgrading.");

        var reference    = paymentService.GenerateReference();
        var subscription = request.UserId.HasValue
            ? Subscription.CreateForUser(request.UserId.Value, request.Tier, reference, amount)
            : Subscription.CreateForClinic(request.ClinicId!.Value, request.Tier, reference, amount);

        await subscriptionRepository.AddAsync(subscription, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(SubscriptionDto.FromDomain(subscription));
    }
}
