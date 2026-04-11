using PawTrack.Domain.LostPets;

namespace PawTrack.Application.LostPets.DTOs;

public sealed record LostPetEventDto(
    string Id,
    string PetId,
    string OwnerId,
    string Status,
    string? Description,
    string? PublicMessage,
    double? LastSeenLat,
    double? LastSeenLng,
    string? RecentPhotoUrl,
    string? ContactName,
    DateTimeOffset LastSeenAt,
    DateTimeOffset ReportedAt,
    DateTimeOffset? ResolvedAt,
    double? ReunionLat,
    double? ReunionLng,
    double? RecoveryDistanceMeters,
    TimeSpan? RecoveryTime,
    string? CantonName,
    decimal? RewardAmount,
    string? RewardNote)
{
    public static LostPetEventDto FromDomain(LostPetEvent e) => new(
        e.Id.ToString(),
        e.PetId.ToString(),
        e.OwnerId.ToString(),
        e.Status.ToString(),
        e.Description,
        e.PublicMessage,
        e.LastSeenLat,
        e.LastSeenLng,
        e.RecentPhotoUrl,
        e.ContactName,
        e.LastSeenAt,
        e.ReportedAt,
        e.ResolvedAt,
        e.ReunionLat,
        e.ReunionLng,
        e.RecoveryDistanceMeters,
        e.RecoveryTime,
        e.CantonName,
        e.RewardAmount,
        e.RewardNote);
}
