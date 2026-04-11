namespace PawTrack.Application.LostPets.Queries.GetRecoveryOverview;

public sealed record RecoveryOverviewDto(
    int TotalReports,
    int RecoveredCount,
    double OverallRecoveryRate,
    IReadOnlyList<RecoveryOverviewByCantonDto> CantonRecovery,
    IReadOnlyList<RecoveryOverviewBySpeciesDto> SpeciesRecovery);

public sealed record RecoveryOverviewByCantonDto(
    string Canton,
    int TotalReports,
    int RecoveredCount,
    double RecoveryRate);

public sealed record RecoveryOverviewBySpeciesDto(
    string Species,
    int RecoveredCount,
    double? MedianRecoveryHours);
