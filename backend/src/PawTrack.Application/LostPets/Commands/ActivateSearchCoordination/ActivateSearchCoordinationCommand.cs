using MediatR;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Common;

namespace PawTrack.Application.LostPets.Commands.ActivateSearchCoordination;

/// <summary>
/// Activates coordinated search mode for a lost-pet event.
/// Generates a 7×7 grid of 300 m zones centred on the last-seen location.
/// Idempotent: if zones already exist the command returns success without re-generating.
/// </summary>
public sealed record ActivateSearchCoordinationCommand(
    Guid LostPetEventId,
    Guid RequestingUserId) : IRequest<Result<int>>;

public sealed class ActivateSearchCoordinationCommandHandler(
    ILostPetRepository lostPetRepository,
    ISearchZoneRepository searchZoneRepository,
    ISearchZoneGenerator searchZoneGenerator,
    IUnitOfWork unitOfWork)
    : IRequestHandler<ActivateSearchCoordinationCommand, Result<int>>
{
    public async Task<Result<int>> Handle(
        ActivateSearchCoordinationCommand request,
        CancellationToken cancellationToken)
    {
        var lostEvent = await lostPetRepository.GetByIdAsync(request.LostPetEventId, cancellationToken);

        if (lostEvent is null)
            return Result.Failure<int>("Lost pet report not found.");

        if (lostEvent.OwnerId != request.RequestingUserId)
            return Result.Failure<int>("Access denied.");

        if (lostEvent.LastSeenLat is null || lostEvent.LastSeenLng is null)
            return Result.Failure<int>("Last-seen coordinates are required to generate search zones.");

        // Idempotent guard: re-uses existing zones if coordination was already activated.
        if (await searchZoneRepository.AnyForLostPetEventAsync(request.LostPetEventId, cancellationToken))
        {
            var existing = await searchZoneRepository.GetByLostPetEventIdAsync(request.LostPetEventId, cancellationToken);
            return Result.Success(existing.Count);
        }

        var zones = searchZoneGenerator.Generate(
            lostPetEventId: request.LostPetEventId,
            centerLat:      lostEvent.LastSeenLat.Value,
            centerLng:      lostEvent.LastSeenLng.Value);

        foreach (var zone in zones)
        {
            await searchZoneRepository.AddAsync(zone, cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(zones.Count);
    }
}
