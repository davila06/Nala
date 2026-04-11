namespace PawTrack.Application.Common.Interfaces;

/// <summary>
/// Read-only source for public recovery statistics aggregations.
/// </summary>
public interface IRecoveryStatsReadRepository
{
    Task<RecoveryStatsRawData> GetRecoveryStatsRawAsync(
        string? species,
        string? breed,
        string? canton,
        CancellationToken cancellationToken = default);

    Task<RecoveryStatsOverviewRawData> GetRecoveryOverviewRawAsync(
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Raw materialized values used to calculate median and percentiles in the application layer.
/// </summary>
public sealed record RecoveryStatsRawData(
    int TotalReports,
    IReadOnlyList<double> RecoveredDistancesMeters,
    IReadOnlyList<double> RecoveryDurationsHours);

public sealed record RecoveryStatsOverviewRawData(
    IReadOnlyList<RecoveryCantonRawItem> CantonRows,
    IReadOnlyList<RecoverySpeciesRawItem> SpeciesRows);

public sealed record RecoveryCantonRawItem(
    string Canton,
    int TotalReports,
    int RecoveredCount);

public sealed record RecoverySpeciesRawItem(
    string Species,
    IReadOnlyList<double> RecoveryHours,
    int RecoveredCount);
