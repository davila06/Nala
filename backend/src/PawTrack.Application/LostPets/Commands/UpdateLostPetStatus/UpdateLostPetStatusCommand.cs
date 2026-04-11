using MediatR;
using PawTrack.Domain.Common;
using PawTrack.Domain.LostPets;

namespace PawTrack.Application.LostPets.Commands.UpdateLostPetStatus;

public sealed record UpdateLostPetStatusCommand(
    Guid LostPetEventId,
    Guid RequestingUserId,
    LostPetStatus NewStatus,
    Guid? ConfirmedSightingId = null) : IRequest<Result<bool>>;
