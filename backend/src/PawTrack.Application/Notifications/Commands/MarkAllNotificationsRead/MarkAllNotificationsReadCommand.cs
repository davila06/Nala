using MediatR;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Notifications.Commands.MarkAllNotificationsRead;

public sealed record MarkAllNotificationsReadCommand(
    Guid UserId) : IRequest<Result<bool>>;

public sealed class MarkAllNotificationsReadCommandHandler(
    INotificationRepository notificationRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<MarkAllNotificationsReadCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(
        MarkAllNotificationsReadCommand request, CancellationToken cancellationToken)
    {
        var unread = await notificationRepository.GetUnreadByUserIdAsync(request.UserId, cancellationToken);

        foreach (var notification in unread)
            notification.MarkAsRead();

        if (unread.Count > 0)
            await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(true);
    }
}
