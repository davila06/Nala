using MediatR;
using PawTrack.Application.Collars.Commands.CreateCollarSafeZone;
using PawTrack.Application.Collars.Interfaces;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Collars.Queries.GetCollarSafeZones;

public sealed record GetCollarSafeZonesQuery(Guid CollarId, Guid RequestingUserId)
    : IRequest<Result<IReadOnlyList<CollarSafeZoneDto>>>;

public sealed class GetCollarSafeZonesQueryHandler(
    ICollarRepository collarRepository,
    ICollarSafeZoneRepository safeZoneRepository)
    : IRequestHandler<GetCollarSafeZonesQuery, Result<IReadOnlyList<CollarSafeZoneDto>>>
{
    public async Task<Result<IReadOnlyList<CollarSafeZoneDto>>> Handle(
        GetCollarSafeZonesQuery request, CancellationToken cancellationToken)
    {
        var collar = await collarRepository.GetByIdAsync(request.CollarId, cancellationToken);
        if (collar is null)
            return Result.Failure<IReadOnlyList<CollarSafeZoneDto>>("Collar no encontrado.");

        if (collar.OwnerId != request.RequestingUserId)
            return Result.Failure<IReadOnlyList<CollarSafeZoneDto>>("Access denied.");

        var zones = await safeZoneRepository.GetByCollarIdAsync(request.CollarId, cancellationToken);
        return Result.Success<IReadOnlyList<CollarSafeZoneDto>>(
            zones.Select(CollarSafeZoneDto.FromDomain).ToList());
    }
}
