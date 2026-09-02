using MediatR;
using PawTrack.Application.Collars.Interfaces;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Collars;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Collars.Commands.RedeemCollarHandoverCode;

public sealed record RedeemCollarHandoverCodeCommand(Guid HandoverCodeId, string Pin, Guid RedeemingUserId)
    : IRequest<Result<RedeemCollarHandoverCodeResultDto>>;

/// <param name="Serial">Released serial — the new owner completes onboarding via the existing activation flow.</param>
public sealed record RedeemCollarHandoverCodeResultDto(string Serial);

public sealed class RedeemCollarHandoverCodeCommandHandler(
    ICollarHandoverCodeRepository handoverRepository,
    ICollarRepository collarRepository,
    ICollarTagRepository collarTagRepository,
    ICollarDeviceCredentialRepository credentialRepository,
    ICollarAuditRepository auditRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<RedeemCollarHandoverCodeCommand, Result<RedeemCollarHandoverCodeResultDto>>
{
    public async Task<Result<RedeemCollarHandoverCodeResultDto>> Handle(
        RedeemCollarHandoverCodeCommand request, CancellationToken cancellationToken)
    {
        var code = await handoverRepository.GetByIdAsync(request.HandoverCodeId, cancellationToken);
        if (code is null)
            return Result.Failure<RedeemCollarHandoverCodeResultDto>("Código no encontrado.");
        if (code.IsRedeemed)
            return Result.Failure<RedeemCollarHandoverCodeResultDto>("Este código ya fue utilizado.");
        if (code.IsCancelled)
            return Result.Failure<RedeemCollarHandoverCodeResultDto>("Este código fue cancelado por el propietario.");
        if (code.IsExpired)
            return Result.Failure<RedeemCollarHandoverCodeResultDto>("Este código expiró.");
        if (code.IsLocked)
            return Result.Failure<RedeemCollarHandoverCodeResultDto>(
                "Demasiados intentos fallidos. Solicita un nuevo código al propietario anterior.");

        var pinHash = CollarDeviceKeyHasher.Compute(request.Pin.Trim());
        if (!string.Equals(pinHash, code.PinHash, StringComparison.Ordinal))
        {
            code.RecordFailedAttempt();
            handoverRepository.Update(code);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            var remaining = Math.Max(0, CollarHandoverCode.MaxAttempts - code.AttemptCount);
            return Result.Failure<RedeemCollarHandoverCodeResultDto>(
                $"PIN incorrecto. Intentos restantes: {remaining}.");
        }

        var collar = await collarRepository.GetByIdAsync(code.CollarId, cancellationToken);
        if (collar is null)
            return Result.Failure<RedeemCollarHandoverCodeResultDto>("Collar no encontrado.");

        var serial = collar.CollarTagSerial;
        if (string.IsNullOrEmpty(serial))
            return Result.Failure<RedeemCollarHandoverCodeResultDto>("Este collar ya no tiene un serial físico asociado.");

        var tag = await collarTagRepository.GetBySerialAsync(serial, cancellationToken);
        if (tag is null || tag.CollarId != collar.Id)
            return Result.Failure<RedeemCollarHandoverCodeResultDto>("Inconsistencia de datos: serial no vinculado a este collar.");

        // Release: revoke device credentials, deactivate the old collar, reset the tag to Unactivated
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

        code.Redeem(request.RedeemingUserId);
        handoverRepository.Update(code);

        await auditRepository.AddAsync(
            CollarAuditEntry.Create(
                CollarAuditEvent.HandoverCompleted,
                $"Serial liberado para el nuevo propietario {request.RedeemingUserId}",
                collarId: collar.Id, serial: serial, userId: request.RedeemingUserId),
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(new RedeemCollarHandoverCodeResultDto(serial));
    }
}
