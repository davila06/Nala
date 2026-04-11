using MediatR;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Notifications.Commands.MarkNotificationRead;

public sealed record MarkNotificationReadCommand(
    Guid NotificationId,
    Guid RequestingUserId) : IRequest<Result<bool>>;

public sealed class MarkNotificationReadCommandHandler(
    INotificationRepository notificationRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<MarkNotificationReadCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(
        MarkNotificationReadCommand request, CancellationToken cancellationToken)
    {
        var notification = await notificationRepository.GetByIdAsync(request.NotificationId, cancellationToken);

        if (notification is null)
            return Result.Failure<bool>("Notification not found.");

        if (notification.UserId != request.RequestingUserId)
            return Result.Failure<bool>("Access denied.");

        notification.MarkAsRead();
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(true);
    }
}
