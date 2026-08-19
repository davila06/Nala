using MediatR;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Common;
using PushEntity = PawTrack.Domain.Notifications.PushSubscription;

namespace PawTrack.Application.Notifications.Commands.RegisterPushSubscription;

public sealed class RegisterPushSubscriptionCommandHandler(
    IPushSubscriptionRepository pushRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<RegisterPushSubscriptionCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(
        RegisterPushSubscriptionCommand request,
        CancellationToken cancellationToken)
    {
        var existing = await pushRepository.GetByEndpointAsync(request.Endpoint, cancellationToken);
        if (existing is not null)
        {
            // Only delete if owned by this user; otherwise ignore silently —
            // an attacker spoofing another user's endpoint must not steal their subscription
            if (existing.UserId == request.UserId)
                await pushRepository.DeleteByEndpointAsync(request.Endpoint, cancellationToken);
        }

        var subscription = PushEntity.Create(request.UserId, request.Endpoint, request.KeysJson);
        await pushRepository.AddAsync(subscription, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(true);
    }
}
