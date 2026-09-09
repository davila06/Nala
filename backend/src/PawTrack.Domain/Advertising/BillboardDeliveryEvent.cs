namespace PawTrack.Domain.Advertising;

public enum BillboardDeliveryEventType { Impression, Click, Conversion }

/// <summary>Privacy-safe, idempotent delivery evidence used for advertiser reporting.</summary>
public sealed class BillboardDeliveryEvent
{
    private BillboardDeliveryEvent() { }

    public Guid Id { get; private set; }
    public Guid BillboardId { get; private set; }
    public BillboardDeliveryEventType EventType { get; private set; }
    public string EventKeyHash { get; private set; } = string.Empty;
    public string VisitorHash { get; private set; } = string.Empty;
    public string? Canton { get; private set; }
    public DateOnly OccurredOn { get; private set; }
    public DateTimeOffset RecordedAt { get; private set; }

    public static BillboardDeliveryEvent Record(
        Guid billboardId,
        BillboardDeliveryEventType eventType,
        string eventKeyHash,
        string visitorHash,
        string? canton = null) => new()
        {
            Id = Guid.CreateVersion7(),
            BillboardId = billboardId,
            EventType = eventType,
            EventKeyHash = eventKeyHash,
            VisitorHash = visitorHash,
            Canton = canton?.Trim(),
            OccurredOn = DateOnly.FromDateTime(DateTime.UtcNow),
            RecordedAt = DateTimeOffset.UtcNow,
        };
}