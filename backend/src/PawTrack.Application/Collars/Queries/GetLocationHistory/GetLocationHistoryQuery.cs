using MediatR;
using PawTrack.Application.Collars.Interfaces;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Collars.Queries.GetLocationHistory;

public sealed record LocationPointDto(double Lat, double Lng, DateTimeOffset RecordedAt);

public sealed record GetLocationHistoryQuery(
    Guid PetId,
    Guid RequestingUserId,
    int Hours = 24,
    int MaxPoints = 500) : IRequest<Result<IReadOnlyList<LocationPointDto>>>;

public sealed class GetLocationHistoryQueryHandler(ICollarRepository collarRepository, IPetRepository petRepository)
    : IRequestHandler<GetLocationHistoryQuery, Result<IReadOnlyList<LocationPointDto>>>
{
    public async Task<Result<IReadOnlyList<LocationPointDto>>> Handle(
        GetLocationHistoryQuery request,
        CancellationToken cancellationToken)
    {
        var pet = await petRepository.GetByIdAsync(request.PetId, cancellationToken);
        if (pet is null || pet.OwnerId != request.RequestingUserId)
            return Result.Failure<IReadOnlyList<LocationPointDto>>("Access denied.");

        var collar = await collarRepository.GetActiveForPetAsync(request.PetId, cancellationToken);
        if (collar is null)
            return Result.Success<IReadOnlyList<LocationPointDto>>([]);

        var since = DateTimeOffset.UtcNow.AddHours(-Math.Clamp(request.Hours, 1, 168));
        var history = await collarRepository.GetLocationHistoryAsync(
            collar.Id, since, request.MaxPoints, cancellationToken);

        return Result.Success<IReadOnlyList<LocationPointDto>>(
            history.Select(l => new LocationPointDto(l.Lat, l.Lng, l.RecordedAt)).ToList());
    }
}
