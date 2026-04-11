using MediatR;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Notifications.Queries.GetNotificationPreferences;

public sealed class GetNotificationPreferencesQueryHandler(
    IUserNotificationPreferencesRepository preferencesRepository)
    : IRequestHandler<GetNotificationPreferencesQuery, Result<NotificationPreferencesDto>>
{
    public async Task<Result<NotificationPreferencesDto>> Handle(
        GetNotificationPreferencesQuery request, CancellationToken cancellationToken)
    {
        var prefs = await preferencesRepository.GetByUserIdAsync(request.UserId, cancellationToken);

        // If no record exists yet the user has all alerts enabled by default
        var dto = prefs is null
            ? new NotificationPreferencesDto(EnablePreventiveAlerts: true)
            : new NotificationPreferencesDto(EnablePreventiveAlerts: prefs.EnablePreventiveAlerts);

        return Result.Success(dto);
    }
}
