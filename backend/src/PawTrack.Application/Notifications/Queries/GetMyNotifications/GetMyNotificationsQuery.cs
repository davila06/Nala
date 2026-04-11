using MediatR;
using PawTrack.Application.Common;
using PawTrack.Application.Notifications.DTOs;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Notifications.Queries.GetMyNotifications;

public sealed record GetMyNotificationsQuery(
    Guid UserId,
    int PageNumber = 1,
    int PageSize = 20) : IRequest<Result<PagedResult<NotificationDto>>>;
