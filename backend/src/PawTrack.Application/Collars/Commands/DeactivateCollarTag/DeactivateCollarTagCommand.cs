using MediatR;
using PawTrack.Application.Collars.Interfaces;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Collars;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Collars.Commands.DeactivateCollarTag;

public sealed record DeactivateCollarTagCommand(string Serial, Guid OwnerId) : IRequest<Result<bool>>;

public sealed class DeactivateCollarTagCommandHandler(
    ICollarTagRepository collarTagRepository,
    ICollarDeviceCredentialRepository credentialRepository,
    ICollarRepository collarRepository,
    ICollarAuditRepository auditRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<DeactivateCollarTagCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(
        DeactivateCollarTagCommand request, CancellationToken cancellationToken)
    {
        var tag = await collarTagRepository.GetBySerialAsync(request.Serial.ToUpperInvariant(), cancellationToken);
        if (tag is null || tag.CollarId is null)
            return Result.Failure<bool>("Serial no encontrado o no está activado.");

        var collar = await collarRepository.GetByIdAsync(tag.CollarId.Value, cancellationToken);
        if (collar is null || collar.OwnerId != request.OwnerId)
            return Result.Failure<bool>("Access denied.");

        // Revoke all active credentials so the device stops being able to ingest
        var credentials = await credentialRepository.GetForCollarAsync(collar.Id, cancellationToken);
        foreach (var cred in credentials.Where(c => c.IsUsable))
        {
            cred.Revoke();
            credentialRepository.Update(cred);
        }

        collar.Deactivate();
        collarRepository.Update(collar);

        tag.Deactivate();
        collarTagRepository.Update(tag);

        await auditRepository.AddAsync(
            CollarAuditEntry.Create(
                CollarAuditEvent.Deactivated,
                "Desvinculado por el propietario",
                collarId: collar.Id, serial: tag.Serial, userId: request.OwnerId),
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(true);
    }
}
