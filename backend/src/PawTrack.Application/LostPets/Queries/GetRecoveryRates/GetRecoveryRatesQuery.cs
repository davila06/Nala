using MediatR;
using PawTrack.Application.Common.Interfaces;

namespace PawTrack.Application.LostPets.Queries.GetRecoveryRates;

public sealed record GetRecoveryRatesQuery(
    string? Species,
    string? Breed,
    string? Canton) : IRequest<RecoveryRatesDto>;

public sealed class GetRecoveryRatesQueryHandler(
    IRecoveryStatsReadRepository repository)
    : IRequestHandler<GetRecoveryRatesQuery, RecoveryRatesDto>
{
    public async Task<RecoveryRatesDto> Handle(
        GetRecoveryRatesQuery request,
        CancellationToken cancellationToken)
    {
        var raw = await repository.GetRecoveryStatsRawAsync(
            request.Species,
            request.Breed,
            request.Canton,
            cancellationToken);

        var recoveredCount = raw.RecoveredDistancesMeters.Count;
        var recoveryRate = raw.TotalReports <= 0
            ? 0
            : (double)recoveredCount / raw.TotalReports;

        return new RecoveryRatesDto(
            TotalReports: raw.TotalReports,
            RecoveredCount: recoveredCount,
            RecoveryRate: Math.Round(recoveryRate, 4),
            MedianRecoveryHours: Median(raw.RecoveryDurationsHours),
            MedianDistanceMeters: Median(raw.RecoveredDistancesMeters),
            P90DistanceMeters: Percentile(raw.RecoveredDistancesMeters, 0.90),
            DataPoints: recoveredCount);
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

    private static double? Percentile(IReadOnlyList<double> values, double percentile)
    {
        if (values.Count == 0) return null;

        var sorted = values.OrderBy(x => x).ToArray();
        var rank = (int)Math.Ceiling(percentile * sorted.Length);
        var index = Math.Clamp(rank - 1, 0, sorted.Length - 1);
        return sorted[index];
    }
}
