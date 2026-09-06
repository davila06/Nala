using MediatR;
using PawTrack.Application.Subscriptions.DTOs;
using PawTrack.Application.Subscriptions.Interfaces;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Municipalities.Interfaces;
using PawTrack.Domain.Audit;
using PawTrack.Domain.Common;
using PawTrack.Domain.Municipalities;
using PawTrack.Domain.Subscriptions;

namespace PawTrack.Application.Subscriptions.Commands.AdminActivateSubscription;

/// <summary>Admin-only: activates a subscription by its ID (no payment reference required).</summary>
public sealed record AdminActivateSubscriptionCommand(Guid SubscriptionId, int BillingMonths = 1)
    : IRequest<Result<SubscriptionDto>>;

public sealed class AdminActivateSubscriptionCommandHandler(
    ISubscriptionRepository subscriptionRepository,
    IClinicRepository clinicRepository,
    IStoreRepository storeRepository,
    IMunicipalProfileRepository municipalRepo,
    IAuditLogRepository auditLog,
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

        var billingMonths = SubscriptionPricing.IsMunicipalTier(sub.Tier)
            ? 12
            : Math.Max(1, request.BillingMonths);
        sub.Activate(billingMonths);
        subscriptionRepository.Update(sub);
        await SyncClinicFeaturedAsync(sub, true, cancellationToken);
        await SyncStoreFeaturedAsync(sub, true, cancellationToken);
        await SyncMunicipalTierAsync(sub, activate: true, cancellationToken);

        await auditLog.AddAsync(AuditLogEntry.Create(
            Guid.Empty,
            AuditAction.SubscriptionActivated,
            "Subscription", request.SubscriptionId.ToString(),
            $"Tier={sub.Tier} Months={billingMonths}"), cancellationToken);

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

    private async Task SyncStoreFeaturedAsync(Subscription sub, bool featured, CancellationToken ct)
    {
        if (sub.UserId is null) return;
        if (sub.Tier is not (SubscriptionTier.StorePlus or SubscriptionTier.StorePartner)) return;
        var store = await storeRepository.GetByUserIdAsync(sub.UserId.Value, ct);
        if (store is null) return;
        store.SetFeatured(featured);
        storeRepository.Update(store);
    }

    private async Task SyncMunicipalTierAsync(Subscription sub, bool activate, CancellationToken ct)
    {
        if (sub.UserId is null || !SubscriptionPricing.IsMunicipalTier(sub.Tier)) return;
        var profile = await municipalRepo.GetByUserIdAsync(sub.UserId.Value, ct);
        if (profile is null) return;
        var muniTier = sub.Tier switch
        {
            SubscriptionTier.MuniBasica => MunicipalTier.Basica,
            SubscriptionTier.MuniFull => MunicipalTier.Full,
            SubscriptionTier.MuniRedRegional => MunicipalTier.RedRegional,
            _ => MunicipalTier.Basica,
        };
        profile.Upgrade(activate ? muniTier : MunicipalTier.Basica, activate ? sub.ExpiresAt : null);
        municipalRepo.Update(profile);
    }
}
