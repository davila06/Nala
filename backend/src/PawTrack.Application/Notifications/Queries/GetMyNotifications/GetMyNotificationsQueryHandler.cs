using MediatR;
using PawTrack.Application.Common;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Notifications.DTOs;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Notifications.Queries.GetMyNotifications;

public sealed class GetMyNotificationsQueryHandler(
    INotificationRepository notificationRepository)
    : IRequestHandler<GetMyNotificationsQuery, Result<PagedResult<NotificationDto>>>
{
    private const int MaxPageSize = 50;

    public async Task<Result<PagedResult<NotificationDto>>> Handle(
        GetMyNotificationsQuery request, CancellationToken cancellationToken)
    {
        var pageSize = Math.Clamp(request.PageSize, 1, MaxPageSize);
        var pageNumber = Math.Max(request.PageNumber, 1);
        var skip = (pageNumber - 1) * pageSize;

        var notifications = await notificationRepository.GetByUserIdAsync(
            request.UserId, skip, pageSize, cancellationToken);

        var unreadCount = await notificationRepository.CountUnreadAsync(request.UserId, cancellationToken);

        // Total count approximation: unread + already fetched read items
        // For MVP we return the page result with a simple total from the unread count
        var dtos = notifications.Select(NotificationDto.FromDomain).ToList();

        return Result.Success(new PagedResult<NotificationDto>(
            dtos,
            unreadCount,  // unread count as badge indicator
            pageNumber,
            pageSize));
    }
}
