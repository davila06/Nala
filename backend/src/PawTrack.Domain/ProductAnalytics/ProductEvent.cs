namespace PawTrack.Domain.ProductAnalytics;

public sealed class ProductEvent
{
    private ProductEvent() { }

    public Guid Id { get; private set; }
    public Guid EventId { get; private set; }
    public string EventName { get; private set; } = string.Empty;
    public string SchemaVersion { get; private set; } = string.Empty;
    public DateTimeOffset OccurredAt { get; private set; }
    public DateTimeOffset ReceivedAt { get; private set; }
    public string AnonymousId { get; private set; } = string.Empty;
    public Guid? UserId { get; private set; }
    public Guid? PetId { get; private set; }
    public string Source { get; private set; } = string.Empty;
    public string? Canton { get; private set; }
    public string? CorrelationId { get; private set; }

    public static ProductEvent Create(
        Guid eventId,
        string eventName,
        string schemaVersion,
        DateTimeOffset occurredAt,
        string anonymousId,
        string source,
        Guid? userId = null,
        Guid? petId = null,
        string? canton = null,
        string? correlationId = null,
        DateTimeOffset? receivedAt = null)
    {
        if (eventId == Guid.Empty) throw new ArgumentException("EventId is required.", nameof(eventId));
        if (string.IsNullOrWhiteSpace(eventName)) throw new ArgumentException("EventName is required.", nameof(eventName));
        if (string.IsNullOrWhiteSpace(schemaVersion)) throw new ArgumentException("SchemaVersion is required.", nameof(schemaVersion));
        if (string.IsNullOrWhiteSpace(anonymousId)) throw new ArgumentException("AnonymousId is required.", nameof(anonymousId));
        if (string.IsNullOrWhiteSpace(source)) throw new ArgumentException("Source is required.", nameof(source));

        return new ProductEvent
        {
            Id = Guid.CreateVersion7(),
            EventId = eventId,
            EventName = eventName.Trim(),
            SchemaVersion = schemaVersion.Trim(),
            OccurredAt = occurredAt,
            ReceivedAt = receivedAt ?? DateTimeOffset.UtcNow,
            AnonymousId = anonymousId.Trim(),
            UserId = userId,
            PetId = petId,
            Source = source.Trim(),
            Canton = string.IsNullOrWhiteSpace(canton) ? null : canton.Trim(),
            CorrelationId = string.IsNullOrWhiteSpace(correlationId) ? null : correlationId.Trim(),
        };
    }
}