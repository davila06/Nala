using MediatR;
using PawTrack.Application.Regulatory.Dtos;
using PawTrack.Application.Regulatory.Interfaces;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Regulatory.Queries;

public sealed record GetNalaTrendsQuery(DateOnly PeriodStart, DateOnly PeriodEnd, string? Canton)
    : IRequest<Result<IReadOnlyList<NalaTrendPointDto>>>;

public sealed class GetNalaTrendsQueryHandler(IRegulatoryReportQueryService reportQueryService)
    : IRequestHandler<GetNalaTrendsQuery, Result<IReadOnlyList<NalaTrendPointDto>>>
{
    public async Task<Result<IReadOnlyList<NalaTrendPointDto>>> Handle(GetNalaTrendsQuery request, CancellationToken ct)
    {
        if (request.PeriodEnd < request.PeriodStart || request.PeriodEnd.DayNumber - request.PeriodStart.DayNumber > 366)
            return Result.Failure<IReadOnlyList<NalaTrendPointDto>>("El periodo de tendencias no es válido.");
        return Result.Success(await reportQueryService.GetTrendsAsync(request.PeriodStart, request.PeriodEnd, request.Canton, ct));
    }
}
