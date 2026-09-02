using MediatR;
using PawTrack.Application.Collars.Interfaces;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Collars;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Collars.Commands.DeactivateCollarLostMode;

/// <param name="Reason">Optional free-text reason, stored in the audit trail only.</param>
public sealed record DeactivateCollarLostModeCommand(Guid CollarId, Guid OwnerId, string? Reason = null)
    : IRequest<Result<bool>>;

public sealed class DeactivateCollarLostModeCommandHandler(
    ICollarRepository collarRepository,
    ICollarAuditRepository auditRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<DeactivateCollarLostModeCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(
        DeactivateCollarLostModeCommand request, CancellationToken cancellationToken)
    {
        var collar = await collarRepository.GetByIdAsync(request.CollarId, cancellationToken);
        if (collar is null)
            return Result.Failure<bool>("Collar no encontrado.");

        if (collar.OwnerId != request.OwnerId)
            return Result.Failure<bool>("Access denied.");

        if (!collar.IsLost)
            return Result.Failure<bool>("El modo perdido no está activo para este collar.");

        collar.DeactivateLostMode();
        collarRepository.Update(collar);

        await auditRepository.AddAsync(
            CollarAuditEntry.Create(
                CollarAuditEvent.LostModeDeactivated,
                string.IsNullOrWhiteSpace(request.Reason) ? "Desactivado por el propietario" : request.Reason.Trim(),
                collarId: collar.Id, serial: collar.CollarTagSerial, userId: request.OwnerId),
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(true);
    }
}
