using PawTrack.Domain.Outbox;

namespace PawTrack.Domain.Stores.Events;

public enum StoreOrderLifecycleAction
{
    OrderPlaced,
    OrderAccepted,
    PaymentReported,
    PaymentVerified,
    ExternalRefundRecorded,
    ReservationExpired,
    OrderRejected,
    StatusChanged,
}

public sealed record StoreOrderLifecycleDomainEvent(
    Guid EventId,
    Guid OrderId,
    Guid StoreId,
    Guid CustomerId,
    StoreOrderLifecycleAction Action,
    StoreOrderStatus Status,
    OrderFulfillmentType FulfillmentType,
    decimal TotalCrc,
    DateTimeOffset OccurredAt) : IOutboxOnlyNotification;
