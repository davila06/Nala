using PawTrack.Domain.Broadcast;

namespace PawTrack.Application.Broadcast.DTOs;

public sealed record BroadcastAttemptDto(
    string Id,
    string LostPetEventId,
    string Channel,
    string Status,
    string? ExternalId,
    string? TrackingUrl,
    int TrackingClicks,
    string? ErrorMessage,
    DateTimeOffset CreatedAt,
    DateTimeOffset? SentAt)
{
    public static BroadcastAttemptDto FromDomain(BroadcastAttempt a) => new(
        a.Id.ToString(),
        a.LostPetEventId.ToString(),
        a.Channel.ToString(),
        a.Status.ToString(),
        a.ExternalId,
        a.TrackingUrl,
        a.TrackingClicks,
        a.ErrorMessage,
        a.CreatedAt,
        a.SentAt);
}
