using MediatR;
using PawTrack.Application.Broadcast.DTOs;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Broadcast.Queries.GetBroadcastStatus;

/// <summary>
/// Returns the broadcast status (all channel attempts + aggregate metrics)
/// for a given lost-pet event. Owner-gated.
/// </summary>
public sealed record GetBroadcastStatusQuery(
    Guid LostPetEventId,
    Guid RequestingUserId) : IRequest<Result<BroadcastStatusDto>>;
