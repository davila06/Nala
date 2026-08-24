using MediatR;
using PawTrack.Application.Common;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Notifications.DTOs;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Notifications.Queries.GetMyNotifications;

/// <summary>
/// Cursor-based notification query. Clients pass <see cref="AfterCursor"/> (the Id of the
/// last item received) to advance the page. O(1) regardless of history size.
/// </summary>
public sealed record GetMyNotificationsCursorQuery(
    Guid UserId,
    string? AfterCursor,
    int PageSize = 20) : IRequest<Result<CursorPagedResult<NotificationDto>>>;

public sealed class GetMyNotificationsCursorQueryHandler(
    INotificationRepository notificationRepository)
    : IRequestHandler<GetMyNotificationsCursorQuery, Result<CursorPagedResult<NotificationDto>>>
{
    private const int MaxPageSize = 50;

    public async Task<Result<CursorPagedResult<NotificationDto>>> Handle(
        GetMyNotificationsCursorQuery request, CancellationToken ct)
    {
        var take = Math.Clamp(request.PageSize, 1, MaxPageSize);
        Guid? afterId = Guid.TryParse(request.AfterCursor, out var parsed) ? parsed : null;

        var items = await notificationRepository.GetByUserIdAfterCursorAsync(
            request.UserId, afterId, take, ct);

        var dtos = items.Select(NotificationDto.FromDomain).ToList();
        return Result.Success(CursorPagedResult<NotificationDto>.From(dtos, take, d => Guid.Parse(d.Id)));
    }
}
