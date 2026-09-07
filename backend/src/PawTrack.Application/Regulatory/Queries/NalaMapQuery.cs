using MediatR;
using PawTrack.Application.Regulatory.Dtos;
using PawTrack.Application.Regulatory.Interfaces;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Regulatory.Queries;

public sealed record GetNalaMapLayersQuery(
    double South,
    double North,
    double West,
    double East) : IRequest<Result<IReadOnlyList<NalaMapCellDto>>>;

public sealed class GetNalaMapLayersQueryHandler(IRegulatoryReportQueryService reportQueryService)
    : IRequestHandler<GetNalaMapLayersQuery, Result<IReadOnlyList<NalaMapCellDto>>>
{
    public async Task<Result<IReadOnlyList<NalaMapCellDto>>> Handle(GetNalaMapLayersQuery request, CancellationToken ct)
    {
        if (request.South < -90 || request.North > 90 || request.West < -180 || request.East > 180
            || request.South >= request.North || request.West >= request.East)
            return Result.Failure<IReadOnlyList<NalaMapCellDto>>("Los límites geográficos no son válidos.");
        if (request.North - request.South > 10 || request.East - request.West > 10)
            return Result.Failure<IReadOnlyList<NalaMapCellDto>>("El área máxima del mapa es de 10 grados.");

        return Result.Success(await reportQueryService.GetMapCellsAsync(
            request.South, request.North, request.West, request.East, ct));
    }
}
