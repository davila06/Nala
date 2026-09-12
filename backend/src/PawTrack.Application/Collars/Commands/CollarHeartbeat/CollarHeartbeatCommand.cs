using MediatR;
using PawTrack.Application.Collars.Interfaces;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Collars;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Collars.Commands.CollarHeartbeat;

public sealed record CollarHeartbeatCommand(
    Guid CollarId,
    string Serial,
    int? BatteryPercent,
    int? SignalDbm = null,
    string? FirmwareVersion = null) : IRequest<Result<bool>>;

public sealed class CollarHeartbeatCommandHandler(
    ICollarTagRepository collarTagRepository,
    ICollarRepository collarRepository,
    ICollarAuditRepository auditRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<CollarHeartbeatCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(
        CollarHeartbeatCommand request, CancellationToken cancellationToken)
    {
        var tag = await collarTagRepository.GetBySerialAsync(request.Serial.ToUpperInvariant(), cancellationToken);
        if (tag is null || tag.CollarId != request.CollarId)
        {
            await auditRepository.AddAsync(
                CollarAuditEntry.Create(
                    CollarAuditEvent.HeartbeatFailed,
                    "Serial no coincide con la credencial del dispositivo en heartbeat",
                    collarId: request.CollarId, serial: request.Serial),
                cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Failure<bool>("Serial mismatch — heartbeat rejected.");
        }

        var collar = await collarRepository.GetByIdAsync(request.CollarId, cancellationToken);
        if (collar is null || !collar.IsActive)
            return Result.Failure<bool>("Collar no encontrado o inactivo.");

        collar.UpdateHeartbeat(request.BatteryPercent);
        collarRepository.Update(collar);

        tag.UpdateLastPing();
        if (!string.IsNullOrWhiteSpace(request.FirmwareVersion))
            tag.UpdateFirmware(request.FirmwareVersion);
        collarTagRepository.Update(tag);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(true);
    }
}
