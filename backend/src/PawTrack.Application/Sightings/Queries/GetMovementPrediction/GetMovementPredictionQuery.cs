using MediatR;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Sightings.DTOs;
using PawTrack.Application.Sightings.Services;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Sightings.Queries.GetMovementPrediction;

/// <summary>
/// Returns a predictive movement projection for the given lost-pet event.
/// The projection is computed from all associated <see cref="PawTrack.Domain.Sightings.Sighting"/>
/// records — no additional tables are involved.
/// </summary>
public sealed record GetMovementPredictionQuery(Guid LostPetEventId)
    : IRequest<Result<MovementPredictionDto>>;

public sealed class GetMovementPredictionQueryHandler(ISightingRepository sightingRepository)
    : IRequestHandler<GetMovementPredictionQuery, Result<MovementPredictionDto>>
{
    public async Task<Result<MovementPredictionDto>> Handle(
        GetMovementPredictionQuery request,
        CancellationToken cancellationToken)
    {
        // Repository returns sightings ordered by SightedAt descending.
        // Reverse to obtain chronological (ascending) order for vector walk.
        var sightingsDesc = await sightingRepository.GetByLostEventIdAsync(
            request.LostPetEventId, cancellationToken);

        var sightingsAsc = sightingsDesc.Reverse().ToList();

        var prediction = MovementProjectionCalculator.Calculate(sightingsAsc);

        return Result.Success(prediction);
    }
}
