using MediatR;
using PawTrack.Application.Collars.Interfaces;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Collars;
using PawTrack.Domain.Common;
using System.Security.Cryptography;

namespace PawTrack.Application.Collars.Commands.GenerateCollarDeviceKey;

public sealed record GenerateCollarDeviceKeyCommand(Guid CollarId, Guid OwnerId)
    : IRequest<Result<GenerateCollarDeviceKeyResultDto>>;

/// <param name="CollarDeviceKey">Raw key shown once — never persisted in plain text.</param>
public sealed record GenerateCollarDeviceKeyResultDto(Guid CollarId, string CollarDeviceKey);

public sealed class GenerateCollarDeviceKeyCommandHandler(
    ICollarRepository collarRepository,
    ICollarDeviceCredentialRepository credentialRepository,
    ICollarAuditRepository auditRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<GenerateCollarDeviceKeyCommand, Result<GenerateCollarDeviceKeyResultDto>>
{
    public async Task<Result<GenerateCollarDeviceKeyResultDto>> Handle(
        GenerateCollarDeviceKeyCommand request, CancellationToken cancellationToken)
    {
        var collar = await collarRepository.GetByIdAsync(request.CollarId, cancellationToken);
        if (collar is null || !collar.IsActive)
            return Result.Failure<GenerateCollarDeviceKeyResultDto>("Collar no encontrado o inactivo.");

        if (collar.OwnerId != request.OwnerId)
            return Result.Failure<GenerateCollarDeviceKeyResultDto>("Access denied.");

        // Revoke any existing active credentials before issuing a new one
        var existing = await credentialRepository.GetForCollarAsync(request.CollarId, cancellationToken);
        var revokedCount = existing.Count(c => c.IsUsable);
        foreach (var old in existing.Where(c => c.IsUsable))
        {
            old.Revoke();
            credentialRepository.Update(old);
        }

        var rawBytes = RandomNumberGenerator.GetBytes(32);
        var rawKey = "ptwk_collar_" + Convert.ToBase64String(rawBytes)
            .Replace("+", "-").Replace("/", "_").Replace("=", "");
        var keyHash = CollarDeviceKeyHasher.Compute(rawKey);

        await credentialRepository.AddAsync(
            CollarDeviceCredential.Create(request.CollarId, keyHash), cancellationToken);

        await auditRepository.AddAsync(
            CollarAuditEntry.Create(
                CollarAuditEvent.DeviceKeyRegenerated,
                $"{revokedCount} credencial(es) anterior(es) revocada(s)",
                collarId: request.CollarId, serial: collar.CollarTagSerial, userId: request.OwnerId),
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(new GenerateCollarDeviceKeyResultDto(request.CollarId, rawKey));
    }
}
