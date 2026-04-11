namespace PawTrack.Application.LostPets.Queries.GetRecoveryRates;

public sealed record RecoveryRatesDto(
    int TotalReports,
    int RecoveredCount,
    double RecoveryRate,
    double? MedianRecoveryHours,
    double? MedianDistanceMeters,
    double? P90DistanceMeters,
    int DataPoints);
