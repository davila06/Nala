using MediatR;
using PawTrack.Application.Regulatory.Dtos;
using PawTrack.Application.Regulatory.Interfaces;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Regulatory.Queries;

public sealed record GetNalaCantonSummaryQuery(DateOnly PeriodStart, DateOnly PeriodEnd)
    : IRequest<Result<IReadOnlyList<NalaCantonSummaryDto>>>;

public sealed record GetNalaInstitutionPerformanceQuery(DateOnly PeriodStart, DateOnly PeriodEnd)
    : IRequest<Result<NalaInstitutionPerformanceDto>>;

public sealed class GetNalaCantonSummaryQueryHandler(IRegulatoryReportQueryService reports)
    : IRequestHandler<GetNalaCantonSummaryQuery, Result<IReadOnlyList<NalaCantonSummaryDto>>>
{
    public async Task<Result<IReadOnlyList<NalaCantonSummaryDto>>> Handle(GetNalaCantonSummaryQuery request, CancellationToken ct) =>
        request.PeriodEnd < request.PeriodStart
            ? Result.Failure<IReadOnlyList<NalaCantonSummaryDto>>("El periodo no es válido.")
            : Result.Success(await reports.GetCantonSummaryAsync(request.PeriodStart, request.PeriodEnd, 5, ct));
}

public sealed class GetNalaInstitutionPerformanceQueryHandler(IRegulatoryReportQueryService reports)
    : IRequestHandler<GetNalaInstitutionPerformanceQuery, Result<NalaInstitutionPerformanceDto>>
{
    public async Task<Result<NalaInstitutionPerformanceDto>> Handle(GetNalaInstitutionPerformanceQuery request, CancellationToken ct) =>
        request.PeriodEnd < request.PeriodStart
            ? Result.Failure<NalaInstitutionPerformanceDto>("El periodo no es válido.")
            : Result.Success(await reports.GetInstitutionPerformanceAsync(request.PeriodStart, request.PeriodEnd, ct));
}
