using MediatR;
using PawTrack.Application.Pets.DTOs;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Pets.Queries.GetPetByMicrochip;

public sealed record GetPetByMicrochipQuery(string ChipId) : IRequest<Result<PetDto>>;
