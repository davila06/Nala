namespace PawTrack.Domain.Notifications;

public sealed class Notification
{
    private Notification() { } // EF Core

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public NotificationType Type { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Body { get; private set; } = string.Empty;
    public bool IsRead { get; private set; }
    public string? RelatedEntityId { get; private set; }
    public DateTimeOffset? ActionConfirmedAt { get; private set; }
    public string? ActionSummary { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public static Notification Create(
        Guid userId,
        NotificationType type,
        string title,
        string body,
        string? relatedEntityId = null)
    {
        return new Notification
        {
            Id = Guid.CreateVersion7(),
            UserId = userId,
            Type = type,
            Title = title.Trim(),
            Body = body.Trim(),
            IsRead = false,
            RelatedEntityId = relatedEntityId,
            ActionConfirmedAt = null,
            ActionSummary = null,
            CreatedAt = DateTimeOffset.UtcNow,
        };
    }

    public void MarkAsRead()
    {
        IsRead = true;
    }

    public void ConfirmAction(string summary)
    {
        ActionSummary = summary.Trim();
        ActionConfirmedAt = DateTimeOffset.UtcNow;
        IsRead = true;
    }
}
