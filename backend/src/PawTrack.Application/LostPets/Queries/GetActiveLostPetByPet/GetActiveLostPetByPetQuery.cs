using MediatR;
using PawTrack.Application.LostPets.DTOs;
using PawTrack.Domain.Common;

namespace PawTrack.Application.LostPets.Queries.GetActiveLostPetByPet;

public sealed record GetActiveLostPetByPetQuery(
    Guid PetId,
    Guid RequestingUserId) : IRequest<Result<LostPetEventDto?>>;
