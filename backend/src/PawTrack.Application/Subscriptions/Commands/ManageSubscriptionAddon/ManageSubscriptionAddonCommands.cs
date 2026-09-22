using MediatR;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Subscriptions.DTOs;
using PawTrack.Application.Subscriptions.Interfaces;
using PawTrack.Domain.Common;
using PawTrack.Domain.Subscriptions;
using PawTrack.Domain.Audit;

namespace PawTrack.Application.Subscriptions.Commands.ManageSubscriptionAddon;

public sealed record CreateSubscriptionAddonCommand(
    Guid SubscriptionId,
    string EntitlementKey,
    decimal Units,
    DateTimeOffset StartsAt,
    DateTimeOffset ExpiresAt,
    decimal? PriceCrc = null,
    Guid ActorId = default) : IRequest<Result<SubscriptionAddonDto>>;

public sealed class CreateSubscriptionAddonCommandHandler(
    ISubscriptionRepository subscriptionRepository,
    ISubscriptionAddonRepository addonRepository,
    IUnitOfWork unitOfWork,
    IAuditLogRepository? auditLog = null)
    : IRequestHandler<CreateSubscriptionAddonCommand, Result<SubscriptionAddonDto>>
{
    public async Task<Result<SubscriptionAddonDto>> Handle(
        CreateSubscriptionAddonCommand request,
        CancellationToken cancellationToken)
    {
        var subscription = await subscriptionRepository.GetByIdAsync(request.SubscriptionId, cancellationToken);
        if (subscription is null)
            return Result.Failure<SubscriptionAddonDto>("Subscription not found.");

        try
        {
            var addon = SubscriptionAddon.Create(
                request.SubscriptionId, request.EntitlementKey, request.Units,
                request.StartsAt, request.ExpiresAt, request.PriceCrc);
            await addonRepository.AddAsync(addon, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            if (auditLog is not null)
            {
                await auditLog.AddAsync(AuditLogEntry.Create(
                    request.ActorId, AuditAction.SubscriptionActivated, "SubscriptionAddon",
                    addon.Id.ToString(), $"Created {addon.EntitlementKey} +{addon.Units}"), cancellationToken);
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }
            return Result.Success(SubscriptionAddonDto.FromDomain(addon));
        }
        catch (ArgumentException exception)
        {
            return Result.Failure<SubscriptionAddonDto>(exception.Message);
        }
    }
}

public sealed record DeactivateSubscriptionAddonCommand(Guid AddonId, Guid ActorId = default)
    : IRequest<Result<SubscriptionAddonDto>>;

public sealed class DeactivateSubscriptionAddonCommandHandler(
    ISubscriptionAddonRepository addonRepository,
    IUnitOfWork unitOfWork,
    IAuditLogRepository? auditLog = null)
    : IRequestHandler<DeactivateSubscriptionAddonCommand, Result<SubscriptionAddonDto>>
{
    public async Task<Result<SubscriptionAddonDto>> Handle(
        DeactivateSubscriptionAddonCommand request,
        CancellationToken cancellationToken)
    {
        var addon = await addonRepository.GetByIdAsync(request.AddonId, cancellationToken);
        if (addon is null)
            return Result.Failure<SubscriptionAddonDto>("Subscription addon not found.");

        addon.Deactivate();
        addonRepository.Update(addon);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        if (auditLog is not null)
        {
            await auditLog.AddAsync(AuditLogEntry.Create(
                request.ActorId, AuditAction.SubscriptionCancelled, "SubscriptionAddon",
                addon.Id.ToString(), $"Deactivated {addon.EntitlementKey} +{addon.Units}"), cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        return Result.Success(SubscriptionAddonDto.FromDomain(addon));
    }
}
