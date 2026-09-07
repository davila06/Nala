using MediatR;
using PawTrack.Application.Regulatory.Dtos;
using PawTrack.Application.Regulatory.Interfaces;
using PawTrack.Domain.Common;
using PawTrack.Domain.Regulatory;

namespace PawTrack.Application.Regulatory.Queries;

public sealed record GetReportPreviewQuery(
    Guid RequestedByUserId,
    ReportType ReportType,
    ExportScope Scope,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    RegulatoryReportFilter Filter) : IRequest<Result<RegulatoryReportPreviewDto>>;

public sealed class GetReportPreviewQueryHandler(
    IReportDefinitionRepository definitionRepository,
    IReportAuthorizationService authorizationService,
    IRegulatoryReportQueryService reportQueryService)
    : IRequestHandler<GetReportPreviewQuery, Result<RegulatoryReportPreviewDto>>
{
    public async Task<Result<RegulatoryReportPreviewDto>> Handle(GetReportPreviewQuery request, CancellationToken ct)
    {
        if (request.PeriodEnd < request.PeriodStart || request.PeriodEnd.DayNumber - request.PeriodStart.DayNumber > 31)
            return Result.Failure<RegulatoryReportPreviewDto>("El preview admite periodos de hasta 31 días.");

        var authorization = await authorizationService.AuthorizeAsync(
            request.RequestedByUserId,
            request.Scope,
            request.Filter.Canton,
            request.Filter.OrganizationId,
            request.ReportType,
            ct);
        if (!authorization.IsAuthorized)
            return Result.Failure<RegulatoryReportPreviewDto>(authorization.Error!);

        var definition = await definitionRepository.GetActiveAsync(request.ReportType, request.Scope, ct);
        if (definition is null)
            return Result.Failure<RegulatoryReportPreviewDto>("No existe una definición activa para este reporte.");

        var data = await reportQueryService.GetReportDataAsync(
            request.ReportType, request.PeriodStart, request.PeriodEnd, request.Filter, ct);
        return Result.Success(new RegulatoryReportPreviewDto(
            request.ReportType,
            definition.SchemaVersion,
            request.PeriodStart,
            request.PeriodEnd,
            data.Rows.Take(100).ToList(),
            data.Rows.Count(row => row.IsSuppressed),
            data.IsSuppressed));
    }
}
