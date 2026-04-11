using MediatR;
using PawTrack.Application.Pets.DTOs;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Pets.Queries.GetPetDetail;

public sealed record GetPetDetailQuery(Guid PetId, Guid RequestingUserId) : IRequest<Result<PetDto>>;
