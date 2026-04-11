using MediatR;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Pets.DTOs;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Pets.Queries.GetPublicPetProfile;

public sealed class GetPublicPetProfileQueryHandler(
    IPetRepository petRepository,
    ILostPetRepository lostPetRepository)
    : IRequestHandler<GetPublicPetProfileQuery, Result<PublicPetProfileDto>>
{
    public async Task<Result<PublicPetProfileDto>> Handle(
        GetPublicPetProfileQuery request, CancellationToken cancellationToken)
    {
        var pet = await petRepository.GetByIdAsync(request.PetId, cancellationToken);

        if (pet is null)
            return Result.Failure<PublicPetProfileDto>("Pet not found.");

        // Fetch the active lost event to expose contactName and activeLostEventId publicly.
        // ContactPhone is intentionally NOT included — that requires authentication.
        var activeLostEvent = pet.Status == Domain.Pets.PetStatus.Lost
            ? await lostPetRepository.GetActiveByPetIdAsync(request.PetId, cancellationToken)
            : null;

        return Result.Success(PublicPetProfileDto.FromDomain(pet, activeLostEvent));
    }
}
