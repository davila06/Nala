using MediatR;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Collars.Queries.GetCollarTagMetrics;

public sealed record GetCollarTagMetricsQuery : IRequest<Result<CollarTagMetricsDto>>;

public sealed class GetCollarTagMetricsQueryHandler(ICollarTagRepository collarTagRepository)
    : IRequestHandler<GetCollarTagMetricsQuery, Result<CollarTagMetricsDto>>
{
    public async Task<Result<CollarTagMetricsDto>> Handle(
        GetCollarTagMetricsQuery request, CancellationToken cancellationToken)
    {
        var metrics = await collarTagRepository.GetMetricsAsync(cancellationToken);
        return Result.Success(metrics);
    }
}
