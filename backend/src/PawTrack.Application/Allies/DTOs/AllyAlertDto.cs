using PawTrack.Domain.Notifications;

namespace PawTrack.Application.Allies.DTOs;

public sealed record AllyAlertDto(
    string NotificationId,
    string Title,
    string Body,
    string? RelatedEntityId,
    bool IsRead,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ActionConfirmedAt,
    string? ActionSummary)
{
    public static AllyAlertDto FromDomain(Notification notification) => new(
        notification.Id.ToString(),
        notification.Title,
        notification.Body,
        notification.RelatedEntityId,
        notification.IsRead,
        notification.CreatedAt,
        notification.ActionConfirmedAt,
        notification.ActionSummary);
}