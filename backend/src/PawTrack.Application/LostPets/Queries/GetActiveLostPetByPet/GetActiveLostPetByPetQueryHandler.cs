using MediatR;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.LostPets.DTOs;
using PawTrack.Domain.Common;

namespace PawTrack.Application.LostPets.Queries.GetActiveLostPetByPet;

public sealed class GetActiveLostPetByPetQueryHandler(
    ILostPetRepository lostPetRepository,
    IPetRepository petRepository)
    : IRequestHandler<GetActiveLostPetByPetQuery, Result<LostPetEventDto?>>
{
    public async Task<Result<LostPetEventDto?>> Handle(
        GetActiveLostPetByPetQuery request, CancellationToken cancellationToken)
    {
        var pet = await petRepository.GetByIdAsync(request.PetId, cancellationToken);

        if (pet is null)
            return Result.Failure<LostPetEventDto?>("Pet not found.");

        if (pet.OwnerId != request.RequestingUserId)
            return Result.Failure<LostPetEventDto?>("Access denied.");

        var report = await lostPetRepository.GetActiveByPetIdAsync(request.PetId, cancellationToken);
        return Result.Success<LostPetEventDto?>(report is null ? null : LostPetEventDto.FromDomain(report));
    }
}
