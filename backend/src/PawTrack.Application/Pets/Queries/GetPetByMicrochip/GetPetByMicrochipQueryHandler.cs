using MediatR;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Pets.DTOs;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Pets.Queries.GetPetByMicrochip;

public sealed class GetPetByMicrochipQueryHandler(IPetRepository petRepository)
    : IRequestHandler<GetPetByMicrochipQuery, Result<PetDto>>
{
    public async Task<Result<PetDto>> Handle(
        GetPetByMicrochipQuery request, CancellationToken cancellationToken)
    {
        var normalized = request.ChipId.Trim().ToUpperInvariant();
        var pet = await petRepository.GetByMicrochipIdAsync(normalized, cancellationToken);

        return pet is null
            ? Result.Failure<PetDto>("Microchip no registrado en PawTrack.")
            : Result.Success(PetDto.FromDomain(pet));
    }
}
