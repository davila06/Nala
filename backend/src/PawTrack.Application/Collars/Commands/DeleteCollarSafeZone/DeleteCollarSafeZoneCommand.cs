using MediatR;
using PawTrack.Application.Collars.Interfaces;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Collars.Commands.DeleteCollarSafeZone;

public sealed record DeleteCollarSafeZoneCommand(Guid SafeZoneId, Guid OwnerId) : IRequest<Result<bool>>;

public sealed class DeleteCollarSafeZoneCommandHandler(
    ICollarSafeZoneRepository safeZoneRepository,
    ICollarRepository collarRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteCollarSafeZoneCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(
        DeleteCollarSafeZoneCommand request, CancellationToken cancellationToken)
    {
        var zone = await safeZoneRepository.GetByIdAsync(request.SafeZoneId, cancellationToken);
        if (zone is null)
            return Result.Failure<bool>("Zona segura no encontrada.");

        var collar = await collarRepository.GetByIdAsync(zone.CollarId, cancellationToken);
        if (collar is null || collar.OwnerId != request.OwnerId)
            return Result.Failure<bool>("Access denied.");

        safeZoneRepository.Remove(zone);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(true);
    }
}
