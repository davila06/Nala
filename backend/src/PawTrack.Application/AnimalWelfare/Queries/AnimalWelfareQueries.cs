using MediatR;
using PawTrack.Application.AnimalWelfare.Dtos;
using PawTrack.Application.AnimalWelfare.Interfaces;
using PawTrack.Application.AnimalWelfare.Routing;
using PawTrack.Application.Common;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Municipalities.Interfaces;
using PawTrack.Domain.Allies;
using PawTrack.Domain.Municipalities;
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
public sealed record GetAssignedWelfareCaseDetailQuery(Guid CaseId, Guid OrganizationUserId)
    : IRequest<Result<AssignedWelfareCaseDetailDto>>;

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

public sealed class GetAssignedWelfareCasesQueryHandler(
    IAnimalWelfareCaseRepository caseRepository,
    IAllyProfileRepository allyProfileRepository,
    IMunicipalProfileRepository municipalProfileRepository)
    : IRequestHandler<GetAssignedWelfareCasesQuery, Result<PagedAnimalWelfareCasesDto>>
{
    public async Task<Result<PagedAnimalWelfareCasesDto>> Handle(GetAssignedWelfareCasesQuery request, CancellationToken ct)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var items = await caseRepository.GetAssignedAsync(request.OrganizationUserId, (page - 1) * pageSize, pageSize, ct);
        var ally = await allyProfileRepository.GetVerifiedByUserIdAsync(request.OrganizationUserId, ct);
        var municipality = await municipalProfileRepository.GetByUserIdAsync(request.OrganizationUserId, ct);
        var visible = items.Where(welfareCase => CanAccessAssignedCase(welfareCase, request.OrganizationUserId, ally, municipality));
        return Result.Success(new PagedAnimalWelfareCasesDto(visible.Select(i => i.ToSummary()).ToList(), page, pageSize));
    }

    private static bool CanAccessAssignedCase(
        AnimalWelfareCase welfareCase,
        Guid userId,
        AllyProfile? ally,
        MunicipalityProfile? municipality)
    {
        if (welfareCase.AssignedOrganizationUserId != userId) return false;
        if (welfareCase.AssignedRole == WelfareReferralRecipientType.Ally.ToString())
            return ally is not null && welfareCase.ApproxLat.HasValue && welfareCase.ApproxLng.HasValue &&
                GeoHelper.DistanceMetres(welfareCase.ApproxLat.Value, welfareCase.ApproxLng.Value,
                    ally.CoverageLat, ally.CoverageLng) <= ally.CoverageRadiusMetres;
        if (welfareCase.AssignedRole == WelfareReferralRecipientType.Municipality.ToString())
            return municipality is { IsActive: true, IsExpired: false } &&
                municipality.AllCantons.Contains(welfareCase.Canton, StringComparer.OrdinalIgnoreCase);
        return false;
    }
}

public sealed class GetAssignedWelfareCaseDetailQueryHandler(
    IAnimalWelfareCaseRepository caseRepository,
    IAnimalWelfareEvidenceRepository evidenceRepository,
    WelfareRoutingService routingService)
    : IRequestHandler<GetAssignedWelfareCaseDetailQuery, Result<AssignedWelfareCaseDetailDto>>
{
    public async Task<Result<AssignedWelfareCaseDetailDto>> Handle(GetAssignedWelfareCaseDetailQuery request, CancellationToken ct)
    {
        var welfareCase = await caseRepository.GetByIdAsync(request.CaseId, ct);
        if (welfareCase is null || welfareCase.AssignedOrganizationUserId != request.OrganizationUserId ||
            !Enum.TryParse<WelfareReferralRecipientType>(welfareCase.AssignedRole, out var recipientType) ||
            !await routingService.IsEligibleRecipientAsync(welfareCase.Canton, welfareCase.ApproxLat, welfareCase.ApproxLng,
                request.OrganizationUserId, recipientType, ct))
            return Result.Failure<AssignedWelfareCaseDetailDto>("Caso asignado no encontrado.");

        var evidence = await evidenceRepository.GetByCaseIdAsync(welfareCase.Id, ct);
        return Result.Success(new AssignedWelfareCaseDetailDto(
            welfareCase.Id,
            welfareCase.PublicCode,
            welfareCase.Type,
            welfareCase.Status,
            welfareCase.Severity,
            welfareCase.Canton,
            welfareCase.DescriptionSanitized,
            welfareCase.ApproxLat,
            welfareCase.ApproxLng,
            welfareCase.CreatedAt,
            evidence.Select(item => new AnimalWelfareEvidenceDto(
                item.Id, item.EvidenceKind, item.ContentType, item.FileSizeBytes, item.IsSensitive, item.UploadedAt)).ToList()));
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
