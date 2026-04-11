using MediatR;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Notifications.Commands.RegisterPushSubscription;

public sealed record RegisterPushSubscriptionCommand(
    Guid UserId,
    string Endpoint,
    string KeysJson) : IRequest<Result<bool>>;
