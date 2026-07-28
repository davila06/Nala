using MediatR;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Sightings.DTOs;
using PawTrack.Domain.Common;
using PawTrack.Domain.LostPets;

namespace PawTrack.Application.Sightings.Queries.GetPublicMapEvents;

public sealed record GetPublicMapEventsQuery(
    double North,
    double South,
    double East,
    double West) : IRequest<Result<IReadOnlyList<PublicMapEventDto>>>;

public sealed class GetPublicMapEventsQueryHandler(
    ISightingRepository sightingRepository,
    ILostPetRepository lostPetRepository,
    IPetRepository petRepository)
    : IRequestHandler<GetPublicMapEventsQuery, Result<IReadOnlyList<PublicMapEventDto>>>
{
    public async Task<Result<IReadOnlyList<PublicMapEventDto>>> Handle(
        GetPublicMapEventsQuery request, CancellationToken cancellationToken)
    {
        // Sequential fetch — both repositories share the same scoped DbContext,
        // which is not thread-safe; parallel Task.WhenAll causes a concurrency exception.
        var sightings = await sightingRepository.GetInBBoxAsync(
            request.North, request.South, request.East, request.West, cancellationToken);

        var lostPets = await lostPetRepository.GetActiveLostPetsInBBoxAsync(
            request.North, request.South, request.East, request.West, cancellationToken);

        // Collect all unique pet IDs and batch-fetch in one query to avoid N+1.
        var allPetIds = sightings.Select(s => s.PetId)
            .Concat(lostPets.Select(lpe => lpe.PetId))
            .Distinct();

        var pets = await petRepository.GetByIdsAsync(allPetIds, cancellationToken);
        var petMap = pets.ToDictionary(p => p.Id);

        var events = new List<PublicMapEventDto>(sightings.Count + lostPets.Count);

        events.AddRange(sightings.Select(s =>
        {
            petMap.TryGetValue(s.PetId, out var pet);
            return PublicMapEventDto.FromSighting(s, pet?.Name, pet?.Species.ToString().ToLowerInvariant());
        }));

        events.AddRange(lostPets
            .Where(lpe => lpe.LastSeenLat is not null && lpe.LastSeenLng is not null)
            .Select(lpe =>
            {
                petMap.TryGetValue(lpe.PetId, out var pet);
                return PublicMapEventDto.FromLostPet(lpe, pet?.Name, pet?.Species.ToString().ToLowerInvariant());
            }));

        // Chronological descending for consistent map rendering
        events.Sort((a, b) => b.OccurredAt.CompareTo(a.OccurredAt));

        return Result.Success<IReadOnlyList<PublicMapEventDto>>(events.AsReadOnly());
    }
}
