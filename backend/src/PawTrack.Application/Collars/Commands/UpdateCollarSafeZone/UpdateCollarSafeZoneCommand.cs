using MediatR;
using PawTrack.Application.Collars.Commands.CreateCollarSafeZone;
using PawTrack.Application.Collars.Interfaces;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Collars.Commands.UpdateCollarSafeZone;

public sealed record UpdateCollarSafeZoneCommand(
    Guid SafeZoneId, Guid OwnerId, string Name, string PolygonJson, bool Enabled)
    : IRequest<Result<CollarSafeZoneDto>>;

public sealed class UpdateCollarSafeZoneCommandHandler(
    ICollarSafeZoneRepository safeZoneRepository,
    ICollarRepository collarRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateCollarSafeZoneCommand, Result<CollarSafeZoneDto>>
{
    public async Task<Result<CollarSafeZoneDto>> Handle(
        UpdateCollarSafeZoneCommand request, CancellationToken cancellationToken)
    {
        var zone = await safeZoneRepository.GetByIdAsync(request.SafeZoneId, cancellationToken);
        if (zone is null)
            return Result.Failure<CollarSafeZoneDto>("Zona segura no encontrada.");

        var collar = await collarRepository.GetByIdAsync(zone.CollarId, cancellationToken);
        if (collar is null || collar.OwnerId != request.OwnerId)
            return Result.Failure<CollarSafeZoneDto>("Access denied.");

        try { zone.Update(request.Name, request.PolygonJson, request.Enabled); }
        catch (ArgumentException ex) { return Result.Failure<CollarSafeZoneDto>(ex.Message); }

        safeZoneRepository.Update(zone);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(CollarSafeZoneDto.FromDomain(zone));
    }
}
