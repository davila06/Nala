using MediatR;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.LostPets.DTOs;
using PawTrack.Domain.Common;

namespace PawTrack.Application.LostPets.Queries.GetSearchZones;

/// <summary>Returns all search zones for the given lost-pet event. Any authenticated user may query.</summary>
public sealed record GetSearchZonesQuery(Guid LostPetEventId) : IRequest<Result<IReadOnlyList<SearchZoneDto>>>;

public sealed class GetSearchZonesQueryHandler(
    ISearchZoneRepository searchZoneRepository,
    ILostPetRepository lostPetRepository)
    : IRequestHandler<GetSearchZonesQuery, Result<IReadOnlyList<SearchZoneDto>>>
{
    public async Task<Result<IReadOnlyList<SearchZoneDto>>> Handle(
        GetSearchZonesQuery request,
        CancellationToken cancellationToken)
    {
        var lostEvent = await lostPetRepository.GetByIdAsync(request.LostPetEventId, cancellationToken);
        if (lostEvent is null)
            return Result.Failure<IReadOnlyList<SearchZoneDto>>("Lost pet report not found.");

        var zones = await searchZoneRepository.GetByLostPetEventIdAsync(request.LostPetEventId, cancellationToken);

        IReadOnlyList<SearchZoneDto> dtos = zones.Select(SearchZoneDto.FromDomain).ToList().AsReadOnly();
        return Result.Success(dtos);
    }
}
