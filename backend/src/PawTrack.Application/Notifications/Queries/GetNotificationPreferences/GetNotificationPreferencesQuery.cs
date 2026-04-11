using MediatR;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Notifications.Queries.GetNotificationPreferences;

public sealed record GetNotificationPreferencesQuery(Guid UserId)
    : IRequest<Result<NotificationPreferencesDto>>;

public sealed record NotificationPreferencesDto(bool EnablePreventiveAlerts);
