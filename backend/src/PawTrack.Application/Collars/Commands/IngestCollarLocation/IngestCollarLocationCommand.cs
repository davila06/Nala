using MediatR;
using PawTrack.Application.Collars.Interfaces;
using PawTrack.Application.Collars.Services;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Collars;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Collars.Commands.IngestCollarLocation;

public sealed record IngestCollarLocationCommand(
    /// <summary>CollarId resolved and injected by CollarDeviceKeyMiddleware.</summary>
    Guid CollarId,
    string Serial,
    double Lat,
    double Lng,
    int? BatteryPercent,
    DateTimeOffset Timestamp,
    int? AccuracyMeters) : IRequest<Result<bool>>;

public sealed class IngestCollarLocationCommandHandler(
    ICollarTagRepository collarTagRepository,
    ICollarRepository collarRepository,
    ICollarAuditRepository auditRepository,
    ILostPetRepository lostPetRepository,
    CollarSafeZoneEvaluationService safeZoneEvaluationService,
    IUnitOfWork unitOfWork)
    : IRequestHandler<IngestCollarLocationCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(
        IngestCollarLocationCommand request, CancellationToken cancellationToken)
    {
        // Verify the serial in the body matches the collar tied to the credential used
        var tag = await collarTagRepository.GetBySerialAsync(request.Serial.ToUpperInvariant(), cancellationToken);
        if (tag is null || tag.CollarId != request.CollarId)
        {
            await auditRepository.AddAsync(
                CollarAuditEntry.Create(
                    CollarAuditEvent.LocationIngestFailed,
                    "Serial no coincide con la credencial usada",
                    collarId: request.CollarId, serial: request.Serial),
                cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Failure<bool>("Serial mismatch — request rejected.");
        }

        var collar = await collarRepository.GetByIdAsync(request.CollarId, cancellationToken);
        if (collar is null || !collar.IsActive)
            return Result.Failure<bool>("Collar no encontrado o inactivo.");

        collar.UpdateLocation(request.Lat, request.Lng, request.BatteryPercent);
        collarRepository.Update(collar);

        await collarRepository.AddLocationAsync(
            CollarLocation.Record(collar.Id, request.Lat, request.Lng, request.AccuracyMeters),
            cancellationToken);

        if (collar.IsLost && collar.LostPetEventId is not null)
        {
            var lostPetEvent = await lostPetRepository.GetByIdAsync(collar.LostPetEventId.Value, cancellationToken);
            if (lostPetEvent is not null)
            {
                lostPetEvent.UpdateLastSeenLocation(request.Lat, request.Lng, request.Timestamp);
                lostPetRepository.Update(lostPetEvent);
            }
        }

        await safeZoneEvaluationService.EvaluateAsync(collar, request.Lat, request.Lng, cancellationToken);

        // Keep LastPingAt up to date on the tag for admin health dashboard
        tag.UpdateLastPing();
        collarTagRepository.Update(tag);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(true);
    }
}
