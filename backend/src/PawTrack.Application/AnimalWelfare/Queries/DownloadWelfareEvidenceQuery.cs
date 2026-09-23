using MediatR;
using PawTrack.Application.AnimalWelfare.Interfaces;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.AnimalWelfare;
using PawTrack.Domain.Common;

namespace PawTrack.Application.AnimalWelfare.Queries;

public sealed record DownloadWelfareEvidenceQuery(Guid EvidenceId, Guid ActorUserId, bool CanAccessAll = false)
    : IRequest<Result<DownloadWelfareEvidenceDto>>;

public sealed record DownloadWelfareEvidenceDto(byte[] Bytes, string ContentType, string FileName);

public sealed class DownloadWelfareEvidenceQueryHandler(
    IAnimalWelfareEvidenceRepository evidenceRepository,
    IAnimalWelfareCaseRepository caseRepository,
    IAnimalWelfareAuditRepository auditRepository,
    IBlobStorageService blobStorage,
    IUnitOfWork unitOfWork)
    : IRequestHandler<DownloadWelfareEvidenceQuery, Result<DownloadWelfareEvidenceDto>>
{
    public async Task<Result<DownloadWelfareEvidenceDto>> Handle(DownloadWelfareEvidenceQuery request, CancellationToken ct)
    {
        var evidence = await evidenceRepository.GetByIdAsync(request.EvidenceId, ct);
        if (evidence is null) return Result.Failure<DownloadWelfareEvidenceDto>("Evidencia no encontrada.");

        var welfareCase = await caseRepository.GetByIdAsync(evidence.CaseId, ct);
        if (welfareCase is null ||
            (!request.CanAccessAll && welfareCase.AssignedOrganizationUserId != request.ActorUserId))
            return Result.Failure<DownloadWelfareEvidenceDto>("Evidencia no encontrada.");

        var bytes = await blobStorage.DownloadAsync(evidence.BlobUrl, ct);
        if (bytes is null) return Result.Failure<DownloadWelfareEvidenceDto>("Archivo de evidencia no disponible.");

        await auditRepository.AddAsync(AnimalWelfareCaseAuditLog.Create(
            evidence.CaseId,
            WelfareAuditAction.DocumentDownloaded,
            request.ActorUserId,
            evidence.Id.ToString()), ct);
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success(new DownloadWelfareEvidenceDto(bytes, evidence.ContentType, $"welfare-evidence-{evidence.Id}"));
    }
}
