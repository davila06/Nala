using MediatR;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Pets.Commands.ReactivatePet;

public sealed record ReactivatePetCommand(Guid PetId, Guid RequestingUserId) : IRequest<Result<bool>>;
