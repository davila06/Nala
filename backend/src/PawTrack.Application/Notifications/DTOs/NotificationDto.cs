using PawTrack.Domain.Notifications;

namespace PawTrack.Application.Notifications.DTOs;

public sealed record NotificationDto(
    string Id,
    string Type,
    string Title,
    string Body,
    bool IsRead,
    string? RelatedEntityId,
    DateTimeOffset CreatedAt)
{
    public static NotificationDto FromDomain(Notification n) => new(
        n.Id.ToString(),
        n.Type.ToString(),
        n.Title,
        n.Body,
        n.IsRead,
        n.RelatedEntityId,
        n.CreatedAt);
}
