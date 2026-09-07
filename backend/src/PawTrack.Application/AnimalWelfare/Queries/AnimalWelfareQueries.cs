using MediatR;
using PawTrack.Application.AnimalWelfare.Dtos;
using PawTrack.Application.AnimalWelfare.Interfaces;
using PawTrack.Domain.AnimalWelfare;
using PawTrack.Domain.Common;

namespace PawTrack.Application.AnimalWelfare.Queries;

public sealed record GetWelfareCaseQueueQuery(
    WelfareCaseStatus? Status,
    WelfareSeverity? Severity,
    string? Canton,
    int Page,
    int PageSize) : IRequest<Result<PagedAnimalWelfareCasesDto>>;

public sealed record GetAssignedWelfareCasesQuery(Guid OrganizationUserId, int Page, int PageSize) : IRequest<Result<PagedAnimalWelfareCasesDto>>;
public sealed record GetPublicWelfareCaseStatusQuery(string PublicCode) : IRequest<Result<PublicAnimalWelfareCaseStatusDto>>;
public sealed record GetWelfareCaseDetailQuery(Guid CaseId) : IRequest<Result<AnimalWelfareCaseDetailDto>>;

public sealed class GetWelfareCaseQueueQueryHandler(IAnimalWelfareCaseRepository caseRepository)
    : IRequestHandler<GetWelfareCaseQueueQuery, Result<PagedAnimalWelfareCasesDto>>
{
    public async Task<Result<PagedAnimalWelfareCasesDto>> Handle(GetWelfareCaseQueueQuery request, CancellationToken ct)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var items = await caseRepository.GetQueueAsync(request.Status, request.Severity, request.Canton, (page - 1) * pageSize, pageSize, ct);
        return Result.Success(new PagedAnimalWelfareCasesDto(items.Select(i => i.ToSummary()).ToList(), page, pageSize));
    }
}

public sealed class GetAssignedWelfareCasesQueryHandler(IAnimalWelfareCaseRepository caseRepository)
    : IRequestHandler<GetAssignedWelfareCasesQuery, Result<PagedAnimalWelfareCasesDto>>
{
    public async Task<Result<PagedAnimalWelfareCasesDto>> Handle(GetAssignedWelfareCasesQuery request, CancellationToken ct)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var items = await caseRepository.GetAssignedAsync(request.OrganizationUserId, (page - 1) * pageSize, pageSize, ct);
        return Result.Success(new PagedAnimalWelfareCasesDto(items.Select(i => i.ToSummary()).ToList(), page, pageSize));
    }
}

public sealed class GetPublicWelfareCaseStatusQueryHandler(IAnimalWelfareCaseRepository caseRepository)
    : IRequestHandler<GetPublicWelfareCaseStatusQuery, Result<PublicAnimalWelfareCaseStatusDto>>
{
    public async Task<Result<PublicAnimalWelfareCaseStatusDto>> Handle(GetPublicWelfareCaseStatusQuery request, CancellationToken ct)
    {
        var welfareCase = await caseRepository.GetByPublicCodeAsync(request.PublicCode, ct);
        return welfareCase is null
            ? Result.Failure<PublicAnimalWelfareCaseStatusDto>("Caso de bienestar no encontrado.")
            : Result.Success(welfareCase.ToPublicStatus());
    }
}

public sealed class GetWelfareCaseDetailQueryHandler(
    IAnimalWelfareCaseRepository caseRepository,
    IAnimalWelfareEvidenceRepository evidenceRepository,
    IAnimalWelfareAuditRepository auditRepository)
    : IRequestHandler<GetWelfareCaseDetailQuery, Result<AnimalWelfareCaseDetailDto>>
{
    public async Task<Result<AnimalWelfareCaseDetailDto>> Handle(GetWelfareCaseDetailQuery request, CancellationToken ct)
    {
        var welfareCase = await caseRepository.GetByIdAsync(request.CaseId, ct);
        if (welfareCase is null) return Result.Failure<AnimalWelfareCaseDetailDto>("Caso de bienestar no encontrado.");

        var evidence = await evidenceRepository.GetByCaseIdAsync(request.CaseId, ct);
        var notes = await auditRepository.GetNotesByCaseIdAsync(request.CaseId, ct);
        var audit = await auditRepository.GetAuditByCaseIdAsync(request.CaseId, ct);

        return Result.Success(new AnimalWelfareCaseDetailDto(
            welfareCase.Id,
            welfareCase.PublicCode,
            welfareCase.Type,
            welfareCase.Status,
            welfareCase.Severity,
            welfareCase.Canton,
            welfareCase.ApproxLat,
            welfareCase.ApproxLng,
            welfareCase.DescriptionSanitized,
            welfareCase.PetId,
            welfareCase.CapturedAnimalId,
            welfareCase.AssignedOrganizationUserId,
            welfareCase.AssignedRole,
            welfareCase.ClosureReason,
            welfareCase.CreatedAt,
            welfareCase.UpdatedAt,
            welfareCase.ClosedAt,
            evidence.Select(e => new AnimalWelfareEvidenceDto(e.Id, e.EvidenceKind, e.ContentType, e.FileSizeBytes, e.IsSensitive, e.UploadedAt)).ToList(),
            notes.Select(n => new AnimalWelfareNoteDto(n.Id, n.AuthorUserId, n.Body, n.CreatedAt)).ToList(),
            audit.Select(a => new AnimalWelfareAuditDto(a.Id, a.Action, a.ActorUserId, a.Details, a.CreatedAt)).ToList()));
    }
}
