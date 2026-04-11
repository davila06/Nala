using MediatR;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Notifications.Commands.UpdateNotificationPreferences;

public sealed record UpdateNotificationPreferencesCommand(
    Guid UserId,
    bool EnablePreventiveAlerts) : IRequest<Result<Unit>>;
