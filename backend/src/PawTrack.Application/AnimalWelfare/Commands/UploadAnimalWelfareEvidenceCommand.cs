using MediatR;
using PawTrack.Application.AnimalWelfare.Dtos;
using PawTrack.Application.AnimalWelfare.Interfaces;
using PawTrack.Application.Common;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.AnimalWelfare;
using PawTrack.Domain.Common;
using System.Security.Cryptography;

namespace PawTrack.Application.AnimalWelfare.Commands;

public sealed record UploadAnimalWelfareEvidenceCommand(
    Guid CaseId,
    Guid? UploadedByUserId,
    byte[] FileBytes,
    string FileName,
    string ContentType,
    WelfareEvidenceKind EvidenceKind,
    bool IsSensitive) : IRequest<Result<AnimalWelfareEvidenceDto>>;

public sealed class UploadAnimalWelfareEvidenceCommandHandler(
    IAnimalWelfareCaseRepository caseRepository,
    IAnimalWelfareEvidenceRepository evidenceRepository,
    IAnimalWelfareAuditRepository auditRepository,
    IBlobStorageService blobStorage,
    IUnitOfWork unitOfWork)
    : IRequestHandler<UploadAnimalWelfareEvidenceCommand, Result<AnimalWelfareEvidenceDto>>
{
    private const string Container = "welfare-evidence";

    public async Task<Result<AnimalWelfareEvidenceDto>> Handle(UploadAnimalWelfareEvidenceCommand request, CancellationToken ct)
    {
        var welfareCase = await caseRepository.GetByIdAsync(request.CaseId, ct);
        if (welfareCase is null) return Result.Failure<AnimalWelfareEvidenceDto>("Caso de bienestar no encontrado.");
        if (request.FileBytes.Length == 0) return Result.Failure<AnimalWelfareEvidenceDto>("El archivo de evidencia es requerido.");

        var sanitizedFileName = BlobHelper.SanitizeFileName(request.FileName);
        var blobName = $"{request.CaseId}/{Guid.CreateVersion7()}-{sanitizedFileName}";
        using var stream = new MemoryStream(request.FileBytes);
        var blobUrl = await blobStorage.UploadAsync(Container, blobName, stream, request.ContentType, ct);
        var hash = Convert.ToHexString(SHA256.HashData(request.FileBytes)).ToLowerInvariant();

        var evidence = AnimalWelfareEvidence.Create(
            request.CaseId,
            blobUrl,
            request.ContentType,
            request.FileBytes.Length,
            request.EvidenceKind,
            request.UploadedByUserId,
            request.IsSensitive,
            hash);

        await evidenceRepository.AddAsync(evidence, ct);
        await auditRepository.AddAsync(AnimalWelfareCaseAuditLog.Create(
            request.CaseId,
            WelfareAuditAction.EvidenceUploaded,
            request.UploadedByUserId,
            request.EvidenceKind.ToString()), ct);
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success(new AnimalWelfareEvidenceDto(
            evidence.Id,
            evidence.EvidenceKind,
            evidence.ContentType,
            evidence.FileSizeBytes,
            evidence.IsSensitive,
            evidence.UploadedAt));
    }
}
