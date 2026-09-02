using MediatR;
using PawTrack.Application.Collars.Interfaces;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Collars;
using PawTrack.Domain.Common;
using System.Security.Cryptography;

namespace PawTrack.Application.Collars.Commands.GenerateCollarHandoverCode;

public sealed record GenerateCollarHandoverCodeCommand(Guid CollarId, Guid OwnerId)
    : IRequest<Result<GenerateCollarHandoverCodeResultDto>>;

/// <param name="Pin">Raw 6-digit PIN shown once — never persisted in plain text.</param>
public sealed record GenerateCollarHandoverCodeResultDto(Guid HandoverCodeId, string Pin, DateTimeOffset ExpiresAt);

public sealed class GenerateCollarHandoverCodeCommandHandler(
    ICollarRepository collarRepository,
    ICollarHandoverCodeRepository handoverRepository,
    ICollarAuditRepository auditRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<GenerateCollarHandoverCodeCommand, Result<GenerateCollarHandoverCodeResultDto>>
{
    public async Task<Result<GenerateCollarHandoverCodeResultDto>> Handle(
        GenerateCollarHandoverCodeCommand request, CancellationToken cancellationToken)
    {
        var collar = await collarRepository.GetByIdAsync(request.CollarId, cancellationToken);
        if (collar is null || !collar.IsActive)
            return Result.Failure<GenerateCollarHandoverCodeResultDto>("Collar no encontrado o inactivo.");

        if (collar.OwnerId != request.OwnerId)
            return Result.Failure<GenerateCollarHandoverCodeResultDto>("Access denied.");

        if (string.IsNullOrEmpty(collar.CollarTagSerial))
            return Result.Failure<GenerateCollarHandoverCodeResultDto>(
                "Solo los collares PawTrack con serial físico se pueden transferir.");

        // Only one active code per collar — superseding a prior one cancels it
        var existing = await handoverRepository.GetActiveForCollarAsync(request.CollarId, cancellationToken);
        if (existing is not null && existing.IsRedeemable)
        {
            existing.Cancel();
            handoverRepository.Update(existing);
        }

        var rawPin = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
        var pinHash = CollarDeviceKeyHasher.Compute(rawPin);
        var code = CollarHandoverCode.Create(request.CollarId, request.OwnerId, pinHash);
        await handoverRepository.AddAsync(code, cancellationToken);

        await auditRepository.AddAsync(
            CollarAuditEntry.Create(
                CollarAuditEvent.HandoverCodeGenerated,
                "Código de transferencia generado",
                collarId: collar.Id, serial: collar.CollarTagSerial, userId: request.OwnerId),
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(new GenerateCollarHandoverCodeResultDto(code.Id, rawPin, code.ExpiresAt));
    }
}
