using MediatR;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Sightings.DTOs;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Sightings.Queries.GetSightingsByPet;

public sealed record GetSightingsByPetQuery(
    Guid PetId,
    Guid RequestingUserId,
    int Page = 1,
    int PageSize = 50) : IRequest<Result<IReadOnlyList<SightingDto>>>;

public sealed class GetSightingsByPetQueryHandler(
    ISightingRepository sightingRepository,
    IPetRepository petRepository,
    ILostPetRepository lostPetRepository,
    ISightingPriorityScorer sightingPriorityScorer)
    : IRequestHandler<GetSightingsByPetQuery, Result<IReadOnlyList<SightingDto>>>
{
    private const int MaxPageSize = 100;

    public async Task<Result<IReadOnlyList<SightingDto>>> Handle(
        GetSightingsByPetQuery request, CancellationToken cancellationToken)
    {
        var pet = await petRepository.GetByIdAsync(request.PetId, cancellationToken);

        if (pet is null)
            return Result.Failure<IReadOnlyList<SightingDto>>("Pet not found.");

        if (pet.OwnerId != request.RequestingUserId)
            return Result.Failure<IReadOnlyList<SightingDto>>("Access denied.");

        var sightings = await sightingRepository.GetByPetIdAsync(
            request.PetId, cancellationToken);
        var activeLostReport = await lostPetRepository.GetActiveByPetIdAsync(
            request.PetId, cancellationToken);

        var pageSize = Math.Clamp(request.PageSize, 1, MaxPageSize);
        var page = Math.Max(1, request.Page);

        var prioritizedSightings = sightings
            .Select(sighting =>
            {
                var priority = sightingPriorityScorer.Score(pet, activeLostReport, sighting);
                return new
                {
                    Sighting = sighting,
                    Priority = priority,
                    Dto = SightingDto.FromDomain(sighting, priority),
                };
            })
            .OrderByDescending(item => item.Priority.Score)
            .ThenByDescending(item => item.Sighting.SightedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(item => item.Dto)
            .ToList()
            .AsReadOnly();

        return Result.Success<IReadOnlyList<SightingDto>>(
            prioritizedSightings);
    }
}
