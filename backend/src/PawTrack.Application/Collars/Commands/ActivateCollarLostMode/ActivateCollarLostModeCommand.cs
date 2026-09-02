using MediatR;
using PawTrack.Application.Collars.Interfaces;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Collars;
using PawTrack.Domain.Common;
using PawTrack.Domain.LostPets;

namespace PawTrack.Application.Collars.Commands.ActivateCollarLostMode;

public sealed record ActivateCollarLostModeCommand(Guid CollarId, Guid OwnerId)
    : IRequest<Result<ActivateCollarLostModeResultDto>>;

public sealed record ActivateCollarLostModeResultDto(Guid LostPetEventId, bool WasNewlyCreated);

public sealed class ActivateCollarLostModeCommandHandler(
    ICollarRepository collarRepository,
    ILostPetRepository lostPetRepository,
    ICollarAuditRepository auditRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<ActivateCollarLostModeCommand, Result<ActivateCollarLostModeResultDto>>
{
    public async Task<Result<ActivateCollarLostModeResultDto>> Handle(
        ActivateCollarLostModeCommand request, CancellationToken cancellationToken)
    {
        var collar = await collarRepository.GetByIdAsync(request.CollarId, cancellationToken);
        if (collar is null || !collar.IsActive)
            return Result.Failure<ActivateCollarLostModeResultDto>("Collar no encontrado o inactivo.");

        if (collar.OwnerId != request.OwnerId)
            return Result.Failure<ActivateCollarLostModeResultDto>("Access denied.");

        if (collar.IsLost)
            return Result.Failure<ActivateCollarLostModeResultDto>("El modo perdido ya está activo para este collar.");

        // Reuse an existing active report for this pet if the owner already filed one
        var existingEvent = await lostPetRepository.GetActiveByPetIdAsync(collar.PetId, cancellationToken);
        var wasNewlyCreated = existingEvent is null;

        var lostPetEvent = existingEvent ?? LostPetEvent.Create(
            collar.PetId,
            collar.OwnerId,
            description: "Activado automáticamente desde el collar GPS",
            lastSeenLat: collar.LastLat,
            lastSeenLng: collar.LastLng,
            lastSeenAt: collar.LastSeenAt ?? DateTimeOffset.UtcNow);

        if (wasNewlyCreated)
            await lostPetRepository.AddAsync(lostPetEvent, cancellationToken);

        collar.ActivateLostMode(lostPetEvent.Id);
        collarRepository.Update(collar);

        await auditRepository.AddAsync(
            CollarAuditEntry.Create(
                CollarAuditEvent.LostModeActivated,
                wasNewlyCreated ? "Reporte de mascota perdida creado automáticamente" : "Vinculado a reporte existente",
                collarId: collar.Id, serial: collar.CollarTagSerial, userId: request.OwnerId),
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(new ActivateCollarLostModeResultDto(lostPetEvent.Id, wasNewlyCreated));
    }
}
