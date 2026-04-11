using MediatR;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Pets.DTOs;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Pets.Queries.GetPetDetail;

public sealed class GetPetDetailQueryHandler(IPetRepository petRepository)
    : IRequestHandler<GetPetDetailQuery, Result<PetDto>>
{
    public async Task<Result<PetDto>> Handle(
        GetPetDetailQuery request, CancellationToken cancellationToken)
    {
        var pet = await petRepository.GetByIdAsync(request.PetId, cancellationToken);

        if (pet is null)
            return Result.Failure<PetDto>("Pet not found.");

        if (pet.OwnerId != request.RequestingUserId)
            return Result.Failure<PetDto>("Access denied.");

        return Result.Success(PetDto.FromDomain(pet));
    }
}
