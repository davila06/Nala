namespace PawTrack.Domain.Notifications;

/// <summary>
/// Stores a browser Web Push subscription for a user.
/// </summary>
public sealed class PushSubscription
{
    private PushSubscription() { } // EF Core

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    /// <summary>The endpoint URL from the browser subscription object.</summary>
    public string Endpoint { get; private set; } = string.Empty;
    /// <summary>JSON-serialized keys block from the browser subscription (p256dh + auth).</summary>
    public string KeysJson { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }

    public static PushSubscription Create(Guid userId, string endpoint, string keysJson) =>
        new()
        {
            Id = Guid.CreateVersion7(),
            UserId = userId,
            Endpoint = endpoint,
            KeysJson = keysJson,
            CreatedAt = DateTimeOffset.UtcNow,
        };
}
