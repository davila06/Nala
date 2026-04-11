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
    ILostPetRepository lostPetRepository)
    : IRequestHandler<GetPublicMapEventsQuery, Result<IReadOnlyList<PublicMapEventDto>>>
{
    public async Task<Result<IReadOnlyList<PublicMapEventDto>>> Handle(
        GetPublicMapEventsQuery request, CancellationToken cancellationToken)
    {
        // Parallel fetch — sightings and active lost pets are independent reads
        var sightingsTask = sightingRepository.GetInBBoxAsync(
            request.North, request.South, request.East, request.West, cancellationToken);

        var lostPetsTask = lostPetRepository.GetActiveLostPetsInBBoxAsync(
            request.North, request.South, request.East, request.West, cancellationToken);

        await Task.WhenAll(sightingsTask, lostPetsTask);

        var events = new List<PublicMapEventDto>(
            sightingsTask.Result.Count + lostPetsTask.Result.Count);

        events.AddRange(sightingsTask.Result.Select(PublicMapEventDto.FromSighting));
        events.AddRange(lostPetsTask.Result
            .Where(lpe => lpe.LastSeenLat is not null && lpe.LastSeenLng is not null)
            .Select(PublicMapEventDto.FromLostPet));

        // Chronological descending for consistent map rendering
        events.Sort((a, b) => b.OccurredAt.CompareTo(a.OccurredAt));

        return Result.Success<IReadOnlyList<PublicMapEventDto>>(events.AsReadOnly());
    }
}
