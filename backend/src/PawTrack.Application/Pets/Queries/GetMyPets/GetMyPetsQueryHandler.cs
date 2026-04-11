using MediatR;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Pets.DTOs;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Pets.Queries.GetMyPets;

public sealed class GetMyPetsQueryHandler(IPetRepository petRepository)
    : IRequestHandler<GetMyPetsQuery, Result<IReadOnlyList<PetSummaryDto>>>
{
    /// <summary>
    /// Hard cap on the number of pets returned per owner per request.
    /// Prevents DoS via memory exhaustion on accounts with bulk-created pets.
    /// </summary>
    private const int MaxResults = 100;

    public async Task<Result<IReadOnlyList<PetSummaryDto>>> Handle(
        GetMyPetsQuery request, CancellationToken cancellationToken)
    {
        var pets = await petRepository.GetByOwnerIdAsync(request.OwnerId, cancellationToken);
        var dtos = pets.Take(MaxResults).Select(PetSummaryDto.FromDomain).ToList().AsReadOnly();
        return Result.Success<IReadOnlyList<PetSummaryDto>>(dtos);
    }
}
