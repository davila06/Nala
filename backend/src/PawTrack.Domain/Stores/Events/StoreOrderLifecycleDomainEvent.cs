using PawTrack.Domain.Outbox;

namespace PawTrack.Domain.Stores.Events;

public sealed record StoreOrderLifecycleDomainEvent(
    Guid EventId,
    Guid OrderId,
    Guid StoreId,
    Guid CustomerId,
    StoreOrderStatus Status,
    OrderFulfillmentType FulfillmentType,
    decimal TotalCrc,
    DateTimeOffset OccurredAt) : IOutboxOnlyNotification;