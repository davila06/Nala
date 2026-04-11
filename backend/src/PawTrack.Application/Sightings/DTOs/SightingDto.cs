using PawTrack.Domain.Sightings;
using PawTrack.Application.Sightings.Scoring;

namespace PawTrack.Application.Sightings.DTOs;

/// <summary>Full sighting detail returned to the authenticated pet owner.</summary>
public sealed record SightingDto(
    string Id,
    string PetId,
    string? LostPetEventId,
    double Lat,
    double Lng,
    string? PhotoUrl,
    string? Note,
    DateTimeOffset SightedAt,
    DateTimeOffset ReportedAt,
    int PriorityScore,
    string PriorityBadge,
    string RecommendedAction)
{
    public static SightingDto FromDomain(Sighting s, SightingPriority priority) => new(
        s.Id.ToString(),
        s.PetId.ToString(),
        s.LostPetEventId?.ToString(),
        s.Lat,
        s.Lng,
        s.PhotoUrl,
        s.Note,
        s.SightedAt,
        s.ReportedAt,
        priority.Score,
        priority.Badge.ToString(),
        priority.RecommendedAction);
}
