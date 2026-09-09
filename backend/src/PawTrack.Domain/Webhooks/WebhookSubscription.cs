namespace PawTrack.Domain.Webhooks;

public sealed class WebhookSubscription
{
    private WebhookSubscription() { }
    public Guid Id { get; private set; }
    public Guid OwnerUserId { get; private set; }
    public string EndpointUrl { get; private set; } = string.Empty;
    public string SecretProtected { get; private set; } = string.Empty;
    public string EventTypesJson { get; private set; } = "[]";
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? DisabledAt { get; private set; }

    public static WebhookSubscription Create(Guid ownerUserId, string endpointUrl, string secretProtected, IEnumerable<string> eventTypes) => new()
    {
        Id = Guid.CreateVersion7(),
        OwnerUserId = ownerUserId,
        EndpointUrl = endpointUrl.Trim(),
        SecretProtected = secretProtected,
        EventTypesJson = System.Text.Json.JsonSerializer.Serialize(eventTypes.Distinct(StringComparer.Ordinal)),
        IsActive = true,
        CreatedAt = DateTimeOffset.UtcNow,
    };

    public bool SubscribesTo(string eventType) => EventTypesJson.Contains($"\"{eventType}\"", StringComparison.Ordinal);
    public void Disable() { IsActive = false; DisabledAt = DateTimeOffset.UtcNow; }
}