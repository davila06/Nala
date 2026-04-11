namespace PawTrack.Application.Sightings.DTOs;

/// <summary>
/// A scored lost-pet candidate returned alongside a new found-pet report.
/// </summary>
public sealed record MatchCandidateDto(
    Guid LostPetEventId,
    Guid PetId,
    string PetName,
    string? PetPhotoUrl,
    double? LastSeenLat,
    double? LastSeenLng,
    DateTimeOffset LastSeenAt,
    int ScorePercent);
