namespace PawTrack.Domain.Stores;

public sealed class StoreOrder
{
    private StoreOrder() { }
    private readonly List<StoreOrderItem> _items = [];

    public Guid Id { get; private set; }
    public Guid StoreId { get; private set; }
    /// <summary>Optional branch/sede this order is attributed to. Null when the store has no locations.</summary>
    public Guid? LocationId { get; private set; }
    public Guid CustomerId { get; private set; }
    public StoreOrderStatus Status { get; private set; }
    public OrderFulfillmentType FulfillmentType { get; private set; }
    /// <summary>8-char SINPE Móvil reference.</summary>
    public string PaymentReference { get; private set; } = string.Empty;
    public decimal TotalCrc { get; private set; }
    public string? DeliveryAddress { get; private set; }
    public string? CustomerNote { get; private set; }
    public string? StoreNote { get; private set; }
    public bool PaymentReportedByCustomer { get; private set; }
    public DateTimeOffset PlacedAt { get; private set; }
    public DateTimeOffset? ConfirmedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public DateTimeOffset? CancelledAt { get; private set; }

    public IReadOnlyList<StoreOrderItem> Items => _items.AsReadOnly();

    // ── Factory ───────────────────────────────────────────────────────────────

    public static StoreOrder Place(
        Guid storeId,
        Guid customerId,
        string paymentReference,
        OrderFulfillmentType fulfillmentType,
        string? deliveryAddress,
        string? customerNote,
        IReadOnlyList<(Guid ProductId, string ProductName, int Qty, decimal UnitPrice)> lines,
        Guid? locationId = null)
    {
        var order = new StoreOrder
        {
            Id = Guid.CreateVersion7(),
            StoreId = storeId,
            LocationId = locationId,
            CustomerId = customerId,
            Status = StoreOrderStatus.PendingPayment,
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
        return order;
    }

    // ── Behaviour ─────────────────────────────────────────────────────────────

    public void ReportPayment()
    {
        if (Status != StoreOrderStatus.PendingPayment)
            throw new InvalidOperationException("Solo se puede reportar el pago de pedidos pendientes.");
        PaymentReportedByCustomer = true;
        Status = StoreOrderStatus.PaymentReported;
    }

    public void Confirm(string? storeNote = null)
    {
        if (Status != StoreOrderStatus.PaymentReported)
            throw new InvalidOperationException("Solo se pueden confirmar pedidos con pago reportado.");
        Status = StoreOrderStatus.Confirmed;
        StoreNote = storeNote?.Trim();
        ConfirmedAt = DateTimeOffset.UtcNow;
    }

    public void UpdateStatus(StoreOrderStatus newStatus, string? storeNote = null)
    {
        if (!IsValidTransition(Status, newStatus))
            throw new InvalidOperationException(
                $"Transición de estado inválida: {Status} → {newStatus}.");

        Status = newStatus;
        if (storeNote is not null) StoreNote = storeNote.Trim();

        if (newStatus is StoreOrderStatus.Delivered)
            CompletedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Allowed forward-only state machine — prevents skipping steps or reversals.</summary>
    private static bool IsValidTransition(StoreOrderStatus from, StoreOrderStatus to) => (from, to) switch
    {
        (StoreOrderStatus.Confirmed, StoreOrderStatus.Preparing) => true,
        (StoreOrderStatus.Confirmed, StoreOrderStatus.Cancelled) => true,
        (StoreOrderStatus.Preparing, StoreOrderStatus.ReadyForPickup) => true,
        (StoreOrderStatus.Preparing, StoreOrderStatus.OutForDelivery) => true,
        (StoreOrderStatus.Preparing, StoreOrderStatus.Cancelled) => true,
        (StoreOrderStatus.ReadyForPickup, StoreOrderStatus.Delivered) => true,
        (StoreOrderStatus.ReadyForPickup, StoreOrderStatus.Cancelled) => true,
        (StoreOrderStatus.OutForDelivery, StoreOrderStatus.Delivered) => true,
        (StoreOrderStatus.OutForDelivery, StoreOrderStatus.Cancelled) => true,
        _ => false,
    };

    public void Cancel(string? reason = null)
    {
        Status = StoreOrderStatus.Cancelled;
        if (reason is not null) StoreNote = reason.Trim();
        CancelledAt = DateTimeOffset.UtcNow;
    }
}
