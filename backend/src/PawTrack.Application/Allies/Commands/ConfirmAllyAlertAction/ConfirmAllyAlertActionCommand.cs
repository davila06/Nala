using MediatR;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Allies.Commands.ConfirmAllyAlertAction;

public sealed record ConfirmAllyAlertActionCommand(
    Guid NotificationId,
    Guid UserId,
    string ActionSummary) : IRequest<Result<bool>>;