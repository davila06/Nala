using MediatR;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Subscriptions.DTOs;
using PawTrack.Application.Subscriptions.Interfaces;
using PawTrack.Domain.Common;
using PawTrack.Domain.Subscriptions;
using PawTrack.Domain.Audit;

namespace PawTrack.Application.Subscriptions.Commands.ScheduleSubscriptionDowngrade;

public sealed record ScheduleSubscriptionDowngradeCommand(
    Guid SubscriptionId,
    Guid RequestingUserId,
    SubscriptionTier TargetTier) : IRequest<Result<SubscriptionDto>>;

public sealed class ScheduleSubscriptionDowngradeCommandHandler(
    ISubscriptionRepository subscriptionRepository,
    ISubscriptionPlanRepository planRepository,
    IPaymentService paymentService,
    IUnitOfWork unitOfWork,
    IAuditLogRepository? auditLog = null)
    : IRequestHandler<ScheduleSubscriptionDowngradeCommand, Result<SubscriptionDto>>
{
    public async Task<Result<SubscriptionDto>> Handle(
        ScheduleSubscriptionDowngradeCommand request,
        CancellationToken cancellationToken)
    {
        var current = await subscriptionRepository.GetByIdAsync(request.SubscriptionId, cancellationToken);
        if (current is null)
            return Result.Failure<SubscriptionDto>("Subscription not found.");

        if (current.UserId != request.RequestingUserId && current.ClinicOwnerId != request.RequestingUserId)
            return Result.Failure<SubscriptionDto>("Access denied.");

        if (!current.IsActive)
            return Result.Failure<SubscriptionDto>("Only an active subscription can be downgraded.");

        if (!IsSupportedDowngrade(current.Tier, request.TargetTier))
            return Result.Failure<SubscriptionDto>("El cambio de plan solicitado no es un downgrade soportado.");

        if (current.ExpiresAt is null)
            return Result.Failure<SubscriptionDto>("The current subscription has no expiry date.");

        var existingPending = current.ClinicId.HasValue
            ? null
            : await subscriptionRepository.GetPendingForUserAsync(request.RequestingUserId, cancellationToken);
        if (existingPending is not null)
            return Result.Failure<SubscriptionDto>("A pending plan change already exists.");

        if (request.TargetTier == SubscriptionTier.Free)
        {
            current.Cancel();
            subscriptionRepository.Update(current);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            await RecordDowngradeAuditAsync(current, request, cancellationToken);
            return Result.Success(SubscriptionDto.FromDomain(current));
        }

        var plan = await planRepository.GetByTierAsync(request.TargetTier, cancellationToken);
        var isAnnual = SubscriptionPricing.IsMunicipalTier(request.TargetTier);
        var amount = isAnnual ? plan?.AnnualPriceCrc : plan?.MonthlyPriceCrc;
        if (plan is null || !plan.IsActive || amount is null)
            return Result.Failure<SubscriptionDto>("The target plan is not available.");

        current.Cancel();
        subscriptionRepository.Update(current);

        var scheduled = current.ClinicId.HasValue
            ? Subscription.CreateScheduledForClinic(
                current.ClinicId.Value,
                request.RequestingUserId,
                request.TargetTier,
                paymentService.GenerateReference(),
                amount.Value,
                current.ExpiresAt.Value,
                isAnnual ? SubscriptionPricing.AnnualTerm : 1)
            : Subscription.CreateScheduledForUser(
                request.RequestingUserId,
                request.TargetTier,
                paymentService.GenerateReference(),
                amount.Value,
                current.ExpiresAt.Value,
                isAnnual ? SubscriptionPricing.AnnualTerm : 1);

        await subscriptionRepository.AddAsync(scheduled, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await RecordDowngradeAuditAsync(current, request, cancellationToken);

        return Result.Success(SubscriptionDto.FromDomain(scheduled));
    }

    private async Task RecordDowngradeAuditAsync(
        Subscription current,
        ScheduleSubscriptionDowngradeCommand request,
        CancellationToken cancellationToken)
    {
        if (auditLog is null) return;
        await auditLog.AddAsync(AuditLogEntry.Create(
            request.RequestingUserId,
            AuditAction.SubscriptionCancelled,
            "Subscription",
            current.Id.ToString(),
            $"Downgrade scheduled: {current.Tier} -> {request.TargetTier}"), cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static bool IsSupportedDowngrade(SubscriptionTier current, SubscriptionTier target) =>
        (current, target) switch
        {
            (SubscriptionTier.UserFamilia, SubscriptionTier.UserPlus) => true,
            (SubscriptionTier.UserPlus, SubscriptionTier.Free) => true,
            (SubscriptionTier.StorePartner, SubscriptionTier.StorePlus) => true,
            (SubscriptionTier.MuniRedRegional, SubscriptionTier.MuniFull) => true,
            (SubscriptionTier.MuniFull, SubscriptionTier.MuniBasica) => true,
            (SubscriptionTier.ClinicPartner, SubscriptionTier.ClinicPlus) => true,
            _ => false,
        };
}
