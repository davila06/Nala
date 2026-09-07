using MediatR;
using PawTrack.Application.Regulatory.Dtos;
using PawTrack.Application.Regulatory.Interfaces;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Regulatory.Queries;

public sealed record GetNalaOverviewQuery(DateOnly PeriodStart, DateOnly PeriodEnd)
    : IRequest<Result<NalaOverviewDto>>;

public sealed record GetPublicImpactStatsQuery(DateOnly PeriodStart, DateOnly PeriodEnd)
    : IRequest<Result<PublicImpactStatsDto>>;

public sealed class GetNalaOverviewQueryHandler(IRegulatoryReportQueryService reportQueryService)
    : IRequestHandler<GetNalaOverviewQuery, Result<NalaOverviewDto>>
{
    public async Task<Result<NalaOverviewDto>> Handle(GetNalaOverviewQuery request, CancellationToken ct)
    {
        if (request.PeriodEnd < request.PeriodStart)
            return Result.Failure<NalaOverviewDto>("El periodo no es válido.");
        return Result.Success(await reportQueryService.GetOverviewAsync(request.PeriodStart, request.PeriodEnd, ct));
    }
}

public sealed class GetPublicImpactStatsQueryHandler(IRegulatoryReportQueryService reportQueryService)
    : IRequestHandler<GetPublicImpactStatsQuery, Result<PublicImpactStatsDto>>
{
    public async Task<Result<PublicImpactStatsDto>> Handle(GetPublicImpactStatsQuery request, CancellationToken ct)
    {
        if (request.PeriodEnd < request.PeriodStart)
            return Result.Failure<PublicImpactStatsDto>("El periodo no es válido.");
        return Result.Success(await reportQueryService.GetPublicImpactStatsAsync(request.PeriodStart, request.PeriodEnd, 5, ct));
    }
}
