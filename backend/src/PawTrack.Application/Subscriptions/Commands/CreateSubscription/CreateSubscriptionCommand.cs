using MediatR;
using PawTrack.Application.Subscriptions.DTOs;
using PawTrack.Domain.Common;
using PawTrack.Domain.Subscriptions;

namespace PawTrack.Application.Subscriptions.Commands.CreateSubscription;

public sealed record CreateSubscriptionCommand(
    Guid? UserId,
    Guid? ClinicId,
    SubscriptionTier Tier) : IRequest<Result<SubscriptionDto>>;
