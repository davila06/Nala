using MediatR;
using PawTrack.Application.Subscriptions.DTOs;
using PawTrack.Application.Subscriptions.Interfaces;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Common;
using PawTrack.Domain.Subscriptions;

namespace PawTrack.Application.Subscriptions.Commands.AdminActivateSubscription;

/// <summary>Admin-only: activates a subscription by its ID (no payment reference required).</summary>
public sealed record AdminActivateSubscriptionCommand(Guid SubscriptionId, int BillingMonths = 1)
    : IRequest<Result<SubscriptionDto>>;

public sealed class AdminActivateSubscriptionCommandHandler(
    ISubscriptionRepository subscriptionRepository,
    IClinicRepository clinicRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<AdminActivateSubscriptionCommand, Result<SubscriptionDto>>
{
    public async Task<Result<SubscriptionDto>> Handle(
        AdminActivateSubscriptionCommand request, CancellationToken cancellationToken)
    {
        var sub = await subscriptionRepository.GetByIdAsync(request.SubscriptionId, cancellationToken);
        if (sub is null)
            return Result.Failure<SubscriptionDto>("Subscription not found.");

        if (sub.Status == SubscriptionStatus.Active)
            return Result.Failure<SubscriptionDto>("Subscription is already active.");

        sub.Activate(request.BillingMonths);
        subscriptionRepository.Update(sub);
        await SyncClinicFeaturedAsync(sub, true, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(SubscriptionDto.FromDomain(sub));
    }

    private async Task SyncClinicFeaturedAsync(Subscription sub, bool featured, CancellationToken ct)
    {
        if (sub.ClinicId is null || sub.Tier < SubscriptionTier.ClinicPlus) return;
        var clinic = await clinicRepository.GetByIdAsync(sub.ClinicId.Value, ct);
        if (clinic is null) return;
        clinic.SetFeatured(featured);
        clinicRepository.Update(clinic);
    }
}
