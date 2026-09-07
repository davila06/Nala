using MediatR;
using PawTrack.Application.Regulatory.Dtos;
using PawTrack.Application.Regulatory.Interfaces;
using PawTrack.Domain.Common;
using PawTrack.Domain.Regulatory;

namespace PawTrack.Application.Regulatory.Queries;

public sealed record GetReportCatalogQuery(ExportScope Scope) : IRequest<Result<IReadOnlyList<ReportDefinitionDto>>>;
public sealed record GetRegulatoryExportStatusQuery(Guid ExportId, Guid RequestedByUserId) : IRequest<Result<RegulatoryExportDto>>;
public sealed record GetMyRegulatoryExportsQuery(Guid RequestedByUserId, int Page, int PageSize) : IRequest<Result<PagedRegulatoryExportsDto>>;

public sealed class GetReportCatalogQueryHandler(IReportDefinitionRepository definitionRepository)
    : IRequestHandler<GetReportCatalogQuery, Result<IReadOnlyList<ReportDefinitionDto>>>
{
    public async Task<Result<IReadOnlyList<ReportDefinitionDto>>> Handle(GetReportCatalogQuery request, CancellationToken ct)
    {
        var definitions = await definitionRepository.GetActiveForScopeAsync(request.Scope, ct);
        return Result.Success<IReadOnlyList<ReportDefinitionDto>>(definitions.Select(d => d.ToDto()).ToList());
    }
}

public sealed class GetRegulatoryExportStatusQueryHandler(
    IRegulatoryExportRepository exportRepository,
    IReportAuthorizationService authorizationService)
    : IRequestHandler<GetRegulatoryExportStatusQuery, Result<RegulatoryExportDto>>
{
    public async Task<Result<RegulatoryExportDto>> Handle(GetRegulatoryExportStatusQuery request, CancellationToken ct)
    {
        var export = await exportRepository.GetByIdAsync(request.ExportId, ct);
        if (export is null)
            return Result.Failure<RegulatoryExportDto>("Export no encontrado.");
        if (export.RequestedByUserId != request.RequestedByUserId)
        {
            var authorization = await authorizationService.AuthorizeAsync(
                request.RequestedByUserId, export.Scope, export.Canton, export.OrganizationId, export.ReportType, ct);
            if (!authorization.IsAuthorized)
                return Result.Failure<RegulatoryExportDto>("Export no encontrado.");
        }
        return Result.Success(export.ToDto());
    }
}

public sealed class GetMyRegulatoryExportsQueryHandler(IRegulatoryExportRepository exportRepository)
    : IRequestHandler<GetMyRegulatoryExportsQuery, Result<PagedRegulatoryExportsDto>>
{
    public async Task<Result<PagedRegulatoryExportsDto>> Handle(GetMyRegulatoryExportsQuery request, CancellationToken ct)
    {
        var page = Math.Max(request.Page, 1);
        var pageSize = Math.Clamp(request.PageSize, 1, 50);
        var exports = await exportRepository.GetByRequesterAsync(request.RequestedByUserId, (page - 1) * pageSize, pageSize, ct);
        return Result.Success(new PagedRegulatoryExportsDto(exports.Select(e => e.ToDto()).ToList(), page, pageSize));
    }
}
