using MediatR;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Pets.Commands.DeletePet;

public sealed record DeletePetCommand(Guid PetId, Guid RequestingUserId) : IRequest<Result<bool>>;
