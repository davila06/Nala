using PawTrack.Domain.Common;
using PawTrack.Domain.Stores.Events;

namespace PawTrack.Domain.Stores;

public sealed class StoreOrder : IHasDomainEvents
{
    private StoreOrder() { }
    private readonly List<StoreOrderItem> _items = [];
    private readonly List<object> _domainEvents = [];

    public Guid Id { get; private set; }
    public Guid StoreId { get; private set; }
    /// <summary>Optional branch/sede this order is attributed to. Null when the store has no locations.</summary>
    public Guid? LocationId { get; private set; }
    public Guid CustomerId { get; private set; }
    public string? IdempotencyKey { get; private set; }
    public string? RequestHash { get; private set; }
    public StoreOrderStatus Status { get; private set; }
    public OrderFulfillmentType FulfillmentType { get; private set; }
    /// <summary>8-char SINPE Móvil reference.</summary>
    public string PaymentReference { get; private set; } = string.Empty;
    public decimal TotalCrc { get; private set; }
    public string? DeliveryAddress { get; private set; }
    public string? CustomerNote { get; private set; }
    public string? StoreNote { get; private set; }
    public bool PaymentReportedByCustomer { get; private set; }
    public string? PaymentVerificationReference { get; private set; }
    public Guid? PaymentVerifiedByUserId { get; private set; }
    public string? RefundReference { get; private set; }
    public Guid? RefundedByUserId { get; private set; }
    public DateTimeOffset? RefundedAt { get; private set; }
    public bool StockReserved { get; private set; }
    public DateTimeOffset? StockReservationExpiresAt { get; private set; }
    public DateTimeOffset PlacedAt { get; private set; }
    public DateTimeOffset? ConfirmedAt { get; private set; }
    public DateTimeOffset? PaymentConfirmedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public DateTimeOffset? CancelledAt { get; private set; }

    public IReadOnlyList<StoreOrderItem> Items => _items.AsReadOnly();
    public IReadOnlyList<object> DomainEvents => _domainEvents.AsReadOnly();
    public void ClearDomainEvents() => _domainEvents.Clear();

    // ── Factory ───────────────────────────────────────────────────────────────

    public static StoreOrder Place(
        Guid storeId,
        Guid customerId,
        string paymentReference,
        OrderFulfillmentType fulfillmentType,
        string? deliveryAddress,
        string? customerNote,
        IReadOnlyList<(Guid ProductId, string ProductName, int Qty, decimal UnitPrice)> lines,
        Guid? locationId = null,
        string? idempotencyKey = null,
        string? requestHash = null)
    {
        if ((idempotencyKey is null) != (requestHash is null))
            throw new ArgumentException("La clave y la huella de idempotencia deben proporcionarse juntas.");
        if (idempotencyKey is not null && (string.IsNullOrWhiteSpace(idempotencyKey) || idempotencyKey.Trim().Length > 200))
            throw new ArgumentException("La clave de idempotencia es requerida y no puede superar 200 caracteres.", nameof(idempotencyKey));
        if (requestHash is not null && requestHash.Length != 64)
            throw new ArgumentException("La huella de idempotencia no tiene un formato válido.", nameof(requestHash));

        var order = new StoreOrder
        {
            Id = Guid.CreateVersion7(),
            StoreId = storeId,
            LocationId = locationId,
            CustomerId = customerId,
            IdempotencyKey = idempotencyKey?.Trim(),
            RequestHash = requestHash,
            Status = StoreOrderStatus.AwaitingStoreAcceptance,
            FulfillmentType = fulfillmentType,
            PaymentReference = paymentReference,
            DeliveryAddress = deliveryAddress?.Trim(),
            CustomerNote = customerNote?.Trim(),
            PlacedAt = DateTimeOffset.UtcNow,
        };

        foreach (var (pid, name, qty, price) in lines)
        {
            var item = StoreOrderItem.Create(order.Id, pid, name, qty, price);
            order._items.Add(item);
        }

        order.TotalCrc = order._items.Sum(i => i.SubtotalCrc);
        order.AddLifecycleEvent();
        return order;
    }

    // ── Behaviour ─────────────────────────────────────────────────────────────

    public void ReportPayment()
    {
        if (Status != StoreOrderStatus.AwaitingPayment)
            throw new InvalidOperationException("Solo se puede reportar el pago después de que la tienda acepte el pedido.");
        PaymentReportedByCustomer = true;
        Status = StoreOrderStatus.PaymentReported;
        AddLifecycleEvent();
    }

    public void Accept(string? storeNote = null)
    {
        if (Status is not (StoreOrderStatus.AwaitingStoreAcceptance or StoreOrderStatus.PendingPayment or StoreOrderStatus.PaymentReported))
            throw new InvalidOperationException("Solo se puede aceptar una solicitud nueva.");
        if (!StockReserved || StockReservationExpiresAt is null)
            throw new InvalidOperationException("El inventario debe reservarse antes de aceptar el pedido.");
        var paymentWasReported = PaymentReportedByCustomer || Status == StoreOrderStatus.PaymentReported;
        PaymentReportedByCustomer |= Status == StoreOrderStatus.PaymentReported;
        Status = paymentWasReported
            ? StoreOrderStatus.PaymentReported
            : StoreOrderStatus.AwaitingPayment;
        StoreNote = storeNote?.Trim();
        ConfirmedAt = DateTimeOffset.UtcNow;
        AddLifecycleEvent();
    }

    public void VerifyManualPayment(Guid verifiedByUserId, string bankReference, string? storeNote = null)
    {
        if (Status == StoreOrderStatus.Paid && PaymentVerifiedByUserId == verifiedByUserId &&
            string.Equals(PaymentVerificationReference, bankReference?.Trim(), StringComparison.Ordinal))
            return;
        if (Status != StoreOrderStatus.PaymentReported)
            throw new InvalidOperationException("Solo se puede verificar un pago reportado por el cliente.");
        if (verifiedByUserId == Guid.Empty)
            throw new ArgumentException("El usuario que verifica el pago es requerido.", nameof(verifiedByUserId));
        if (string.IsNullOrWhiteSpace(bankReference) || bankReference.Trim().Length > 200)
            throw new ArgumentException("La referencia bancaria es requerida y no puede superar 200 caracteres.", nameof(bankReference));
        if (!StockReserved)
            throw new InvalidOperationException("La reserva de inventario venció; el pedido debe revisarse.");
        Status = StoreOrderStatus.Paid;
        StockReserved = false;
        StockReservationExpiresAt = null;
        StoreNote = storeNote?.Trim();
        PaymentVerificationReference = bankReference.Trim();
        PaymentVerifiedByUserId = verifiedByUserId;
        PaymentConfirmedAt = DateTimeOffset.UtcNow;
        AddLifecycleEvent();
    }

    public void RecordManualRefund(Guid refundedByUserId, string refundReference, string? reason = null)
    {
        if (Status == StoreOrderStatus.Refunded && RefundedByUserId == refundedByUserId &&
            string.Equals(RefundReference, refundReference?.Trim(), StringComparison.Ordinal))
            return;
        if (Status != StoreOrderStatus.Paid)
            throw new InvalidOperationException("Solo un pedido pagado puede registrar una devolución manual.");
        if (refundedByUserId == Guid.Empty)
            throw new ArgumentException("El usuario que registra la devolución es requerido.", nameof(refundedByUserId));
        if (string.IsNullOrWhiteSpace(refundReference) || refundReference.Trim().Length > 200)
            throw new ArgumentException("La referencia de devolución es requerida y no puede superar 200 caracteres.", nameof(refundReference));

        Status = StoreOrderStatus.Refunded;
        RefundReference = refundReference.Trim();
        RefundedByUserId = refundedByUserId;
        RefundedAt = DateTimeOffset.UtcNow;
        if (!string.IsNullOrWhiteSpace(reason)) StoreNote = reason.Trim();
        AddLifecycleEvent();
    }

    public void MarkStockReserved(DateTimeOffset expiresAt)
    {
        if (Status is not (StoreOrderStatus.AwaitingStoreAcceptance or StoreOrderStatus.PendingPayment or StoreOrderStatus.PaymentReported))
            throw new InvalidOperationException("Solo una solicitud nueva puede reservar inventario.");
        if (StockReserved)
            throw new InvalidOperationException("El inventario del pedido ya está reservado.");
        if (expiresAt <= DateTimeOffset.UtcNow)
            throw new ArgumentOutOfRangeException(nameof(expiresAt));
        StockReserved = true;
        StockReservationExpiresAt = expiresAt;
    }

    public bool ReleaseStockReservation()
    {
        if (!StockReserved) return false;
        StockReserved = false;
        StockReservationExpiresAt = null;
        return true;
    }

    public void ExpireStockReservation(DateTimeOffset now)
    {
        if (!StockReserved || StockReservationExpiresAt > now)
            throw new InvalidOperationException("La reserva del pedido todavía está vigente.");
        if (Status is not (StoreOrderStatus.AwaitingPayment or StoreOrderStatus.PaymentReported))
            throw new InvalidOperationException("Solo un pedido pendiente de pago puede vencer.");
        Status = StoreOrderStatus.Expired;
        StockReserved = false;
        StockReservationExpiresAt = null;
        CancelledAt = now;
        AddLifecycleEvent();
    }

    public void Reject(string reason)
    {
        if (Status is not (StoreOrderStatus.AwaitingStoreAcceptance or StoreOrderStatus.AwaitingPayment or StoreOrderStatus.PaymentReported))
            throw new InvalidOperationException("Solo se pueden rechazar solicitudes pendientes.");
        if (string.IsNullOrWhiteSpace(reason))
            throw new InvalidOperationException("El rechazo debe incluir un motivo.");

        Status = StoreOrderStatus.Rejected;
        StoreNote = reason.Trim();
        CancelledAt = DateTimeOffset.UtcNow;
    }

    public void UpdateStatus(StoreOrderStatus newStatus, string? storeNote = null)
    {
        if (!IsValidTransition(Status, newStatus))
            throw new InvalidOperationException(
                $"Transición de estado inválida: {Status} → {newStatus}.");
        if (newStatus is StoreOrderStatus.Cancelled && string.IsNullOrWhiteSpace(storeNote))
            throw new InvalidOperationException("La cancelación debe incluir un motivo.");

        Status = newStatus;
        if (storeNote is not null) StoreNote = storeNote.Trim();

        if (newStatus is StoreOrderStatus.Delivered)
            CompletedAt = DateTimeOffset.UtcNow;

        AddLifecycleEvent();
    }

    /// <summary>Allowed forward-only state machine — prevents skipping steps, reversals, and fulfillment mismatches.</summary>
    private bool IsValidTransition(StoreOrderStatus from, StoreOrderStatus to) => (from, to) switch
    {
        (StoreOrderStatus.AwaitingStoreAcceptance, StoreOrderStatus.Rejected) => true,
        (StoreOrderStatus.AwaitingPayment, StoreOrderStatus.Rejected) => true,
        (StoreOrderStatus.PendingPayment, StoreOrderStatus.Rejected) => true,
        (StoreOrderStatus.PaymentReported, StoreOrderStatus.Rejected) => true,
        (StoreOrderStatus.AwaitingStoreAcceptance, StoreOrderStatus.Cancelled) => true,
        (StoreOrderStatus.AwaitingPayment, StoreOrderStatus.Cancelled) => true,
        (StoreOrderStatus.PaymentReported, StoreOrderStatus.Cancelled) => true,
        (StoreOrderStatus.Paid, StoreOrderStatus.Preparing) => true,
        (StoreOrderStatus.Confirmed, StoreOrderStatus.Preparing) => true,
        (StoreOrderStatus.Confirmed, StoreOrderStatus.Cancelled) => true,
        (StoreOrderStatus.Preparing, StoreOrderStatus.ReadyForPickup)
            when FulfillmentType == OrderFulfillmentType.Pickup => true,
        (StoreOrderStatus.Preparing, StoreOrderStatus.OutForDelivery)
            when FulfillmentType == OrderFulfillmentType.Delivery => true,
        (StoreOrderStatus.Preparing, StoreOrderStatus.Cancelled) => true,
        (StoreOrderStatus.ReadyForPickup, StoreOrderStatus.Delivered)
            when FulfillmentType == OrderFulfillmentType.Pickup => true,
        (StoreOrderStatus.ReadyForPickup, StoreOrderStatus.Cancelled)
            when FulfillmentType == OrderFulfillmentType.Pickup => true,
        (StoreOrderStatus.OutForDelivery, StoreOrderStatus.Delivered)
            when FulfillmentType == OrderFulfillmentType.Delivery => true,
        (StoreOrderStatus.OutForDelivery, StoreOrderStatus.Cancelled)
            when FulfillmentType == OrderFulfillmentType.Delivery => true,
        _ => false,
    };

    public void Cancel(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new InvalidOperationException("La cancelación debe incluir un motivo.");

        Status = StoreOrderStatus.Cancelled;
        StoreNote = reason.Trim();
        CancelledAt = DateTimeOffset.UtcNow;
        AddLifecycleEvent();
    }

    private void AddLifecycleEvent() => _domainEvents.Add(new StoreOrderLifecycleDomainEvent(
        Guid.CreateVersion7(), Id, StoreId, CustomerId, Status, FulfillmentType, TotalCrc, DateTimeOffset.UtcNow));
}
