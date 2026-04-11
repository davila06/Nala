using MediatR;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Common;
using PawTrack.Domain.Notifications;

namespace PawTrack.Application.Allies.Commands.ConfirmAllyAlertAction;

public sealed class ConfirmAllyAlertActionCommandHandler(
    INotificationRepository notificationRepository,
    IAllyProfileRepository allyProfileRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<ConfirmAllyAlertActionCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(
        ConfirmAllyAlertActionCommand request,
        CancellationToken cancellationToken)
    {
        var verifiedProfile = await allyProfileRepository.GetVerifiedByUserIdAsync(request.UserId, cancellationToken);
        if (verifiedProfile is null)
            return Result.Failure<bool>("Only verified allies can confirm alert actions.");

        var notification = await notificationRepository.GetByIdAsync(request.NotificationId, cancellationToken);
        if (notification is null)
            return Result.Failure<bool>("Notification not found.");

        if (notification.UserId != request.UserId)
            return Result.Failure<bool>("Access denied.");

        if (notification.Type != NotificationType.VerifiedAllyAlert)
            return Result.Failure<bool>("Only ally alerts can record actions.");

        notification.ConfirmAction(request.ActionSummary);
        notificationRepository.Update(notification);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(true);
    }
}