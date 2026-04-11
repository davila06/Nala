namespace PawTrack.Application.Sightings.DTOs;

/// <summary>
/// Public-safe projection of a FoundPetReport.
/// ContactName and ContactPhone are intentionally excluded — they are PII.
/// </summary>
public sealed record FoundPetReportDto(
    Guid Id,
    string FoundSpecies,
    string? BreedEstimate,
    string? ColorDescription,
    string? SizeEstimate,
    double FoundLat,
    double FoundLng,
    string? PhotoUrl,
    string? Note,
    string Status,
    int? MatchScore,
    DateTimeOffset ReportedAt);
