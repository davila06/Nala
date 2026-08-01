using MediatR;
using PawTrack.Application.Common.Interfaces;

namespace PawTrack.Application.LostPets.Queries.GetRecoveryOverview;

public sealed record GetRecoveryOverviewQuery : IRequest<RecoveryOverviewDto>;

public sealed class GetRecoveryOverviewQueryHandler(
    IRecoveryStatsReadRepository repository)
    : IRequestHandler<GetRecoveryOverviewQuery, RecoveryOverviewDto>
{
    public async Task<RecoveryOverviewDto> Handle(
        GetRecoveryOverviewQuery request,
        CancellationToken cancellationToken)
    {
        var raw = await repository.GetRecoveryOverviewRawAsync(cancellationToken);

        // Totals from ALL raw rows before the display-only Take(20) truncation
        var totalReports = raw.CantonRows.Sum(x => x.TotalReports);
        var recoveredCount = raw.CantonRows.Sum(x => x.RecoveredCount);
        var overallRate = totalReports == 0 ? 0 : (double)recoveredCount / totalReports;

        var cantonRows = raw.CantonRows
            .Select(row => new RecoveryOverviewByCantonDto(
                row.Canton,
                row.TotalReports,
                row.RecoveredCount,
                row.TotalReports == 0 ? 0 : (double)row.RecoveredCount / row.TotalReports))
            .OrderByDescending(x => x.RecoveryRate)
            .ThenByDescending(x => x.TotalReports)
            .Take(20)
            .ToArray();

        var speciesRows = raw.SpeciesRows
            .Select(row => new RecoveryOverviewBySpeciesDto(
                row.Species,
                row.RecoveredCount,
                Median(row.RecoveryHours)))
            .OrderBy(x => x.MedianRecoveryHours ?? double.MaxValue)
            .ToArray();

        return new RecoveryOverviewDto(
            totalReports,
            recoveredCount,
            Math.Round(overallRate, 4),
            cantonRows,
            speciesRows);
    }

    private static double? Median(IReadOnlyList<double> values)
    {
        if (values.Count == 0) return null;

        var sorted = values.OrderBy(x => x).ToArray();
        var middle = sorted.Length / 2;

        if (sorted.Length % 2 == 1)
            return sorted[middle];

        return (sorted[middle - 1] + sorted[middle]) / 2.0;
    }
}
