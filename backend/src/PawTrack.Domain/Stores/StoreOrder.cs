namespace PawTrack.Domain.Stores;

public sealed class StoreOrder
{
    private StoreOrder() { }
    private readonly List<StoreOrderItem> _items = [];

    public Guid Id { get; private set; }
    public Guid StoreId { get; private set; }
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
        IReadOnlyList<(Guid ProductId, string ProductName, int Qty, decimal UnitPrice)> lines)
    {
        var order = new StoreOrder
        {
            Id = Guid.CreateVersion7(),
            StoreId = storeId,
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
        PaymentReportedByCustomer = true;
        Status = StoreOrderStatus.PaymentReported;
    }

    public void Confirm(string? storeNote = null)
    {
        Status = StoreOrderStatus.Confirmed;
        StoreNote = storeNote?.Trim();
        ConfirmedAt = DateTimeOffset.UtcNow;
    }

    public void UpdateStatus(StoreOrderStatus newStatus, string? storeNote = null)
    {
        Status = newStatus;
        if (storeNote is not null) StoreNote = storeNote.Trim();

        if (newStatus is StoreOrderStatus.Delivered)
            CompletedAt = DateTimeOffset.UtcNow;
    }

    public void Cancel(string? reason = null)
    {
        Status = StoreOrderStatus.Cancelled;
        if (reason is not null) StoreNote = reason.Trim();
        CancelledAt = DateTimeOffset.UtcNow;
    }
}
