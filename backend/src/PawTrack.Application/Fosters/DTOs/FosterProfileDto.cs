using PawTrack.Domain.Pets;

namespace PawTrack.Application.Fosters.DTOs;

public sealed record FosterProfileDto(
    string UserId,
    string FullName,
    double HomeLat,
    double HomeLng,
    IReadOnlyList<PetSpecies> AcceptedSpecies,
    string? SizePreference,
    int MaxDays,
    bool IsAvailable,
    DateTimeOffset? AvailableUntil,
    int TotalFostersCompleted);

/// <summary>
/// Public projection returned by GET /api/fosters/suggestions/….
/// <para>
/// <b>Privacy note:</b> <c>UserId</c> is intentionally NOT exposed — returning the
/// raw GUID would allow any caller to enumerate volunteer identities by sweeping
/// GPS coordinates (cross-reference across found-pet reports → full profile map).
/// Backend contact initiation uses the <c>FoundPetReportId</c> + slot position; the
/// client never needs the underlying GUID.
/// </para>
/// </summary>
public sealed record FosterSuggestionDto(
    string VolunteerName,
    double DistanceMetres,
    string DistanceLabel,
    string? SizePreference,
    int MaxDays,
    bool SpeciesMatch);
