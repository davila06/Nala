namespace PawTrack.Application.Regulatory.Dtos;

public sealed record NalaOverviewDto(
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    int ActiveLostPets,
    int ReunitedPets,
    int CapturedAnimals,
    int AvailableAdoptions,
    int AdoptedAnimals,
    int OpenWelfareCases,
    int VerifiedMicrochips,
    int ValidCertificates,
    DateTimeOffset GeneratedAt,
    bool IsSuppressed);

public sealed record PublicImpactStatsDto(
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    int ReunitedPets,
    int AdoptedAnimals,
    int ResolvedWelfareCases,
    bool IsSuppressed,
    DateTimeOffset GeneratedAt);

public sealed record NalaMapCellDto(
    double Latitude,
    double Longitude,
    string Layer,
    string Canton,
    int Count,
    bool IsSuppressed);

public sealed record NalaTrendPointDto(
    DateOnly Date,
    int LostReports,
    int ReunitedPets,
    int Captures,
    int AdoptedAnimals,
    int WelfareCases);

public sealed record NalaCantonSummaryDto(
    string Canton,
    int Captures,
    int ReunitedPets,
    int AdoptedAnimals,
    int WelfareCases,
    bool IsSuppressed);

public sealed record NalaInstitutionPerformanceDto(
    int VerifiedClinics,
    int ActiveMunicipalities,
    int VerifiedAllies,
    int OpenWelfareCases,
    int AssignedWelfareCases,
    double? MedianWelfareClosureHours);
