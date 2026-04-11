using MediatR;
using PawTrack.Application.Broadcast.DTOs;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Broadcast.Commands.BroadcastLostPet;

/// <summary>
/// Triggers a multi-channel broadcast for an active lost-pet report.
/// Idempotent: can be called multiple times; each call creates new
/// <c>BroadcastAttempt</c> records (re-broadcast / retry semantic).
/// </summary>
public sealed record BroadcastLostPetCommand(
    Guid LostPetEventId,
    Guid RequestingUserId) : IRequest<Result<IReadOnlyList<BroadcastAttemptDto>>>;
