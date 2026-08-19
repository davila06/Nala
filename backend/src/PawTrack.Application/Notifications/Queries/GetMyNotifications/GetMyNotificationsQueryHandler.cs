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

        // 2 queries (items + COUNT / unread) instead of 3 separate round-trips
        var (notifications, totalCount, unreadCount) = await notificationRepository
            .GetPagedWithCountsAsync(request.UserId, skip, pageSize, cancellationToken);
        var dtos = notifications.Select(NotificationDto.FromDomain).ToList();

        return Result.Success(new PagedResult<NotificationDto>(
            dtos,
            totalCount,
            pageNumber,
            pageSize)
        {
            // Expose unread count as an extension so the UI badge doesn't need a separate request
            UnreadCount = unreadCount,
        });
    }
}
