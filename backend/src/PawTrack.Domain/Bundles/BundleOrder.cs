namespace PawTrack.Domain.Bundles;

/// <summary>
/// An on-demand bundle order: customer pre-pays, we source and ship the
/// Tractive collar together with a 12-month PawTrack Plus subscription.
/// </summary>
public sealed class BundleOrder
{
    private BundleOrder() { } // EF Core

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public CollarModel CollarModel { get; private set; }
    public BundleOrderStatus Status { get; private set; }

    /// <summary>8-char SINPE Móvil payment reference shown to the customer.</summary>
    public string PaymentReference { get; private set; } = string.Empty;
    public decimal AmountCrc { get; private set; }

    // ── Shipping ──────────────────────────────────────────────────────────────
    public string ShippingFullName { get; private set; } = string.Empty;
    public string ShippingAddress { get; private set; } = string.Empty;
    public string ShippingCanton { get; private set; } = string.Empty;
    public string ShippingPhone { get; private set; } = string.Empty;
    public string? DeliveryNotes { get; private set; }

    // ── Fulfillment ───────────────────────────────────────────────────────────
    /// <summary>Carrier tracking number (Correos, Servientrega, etc.) set at ship time.</summary>
    public string? TrackingNumber { get; private set; }
    /// <summary>Internal admin notes (purchase order, supplier details, etc.).</summary>
    public string? AdminNotes { get; private set; }
    /// <summary>FK to the UserPlus subscription created when payment is confirmed.</summary>
    public Guid? ActivatedSubscriptionId { get; private set; }

    // ── Timestamps ────────────────────────────────────────────────────────────
    public bool PaymentReportedByUser { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? PaidAt { get; private set; }
    public DateTimeOffset? SourcedAt { get; private set; }
    public DateTimeOffset? ShippedAt { get; private set; }
    public DateTimeOffset? DeliveredAt { get; private set; }
    public DateTimeOffset? CancelledAt { get; private set; }

    // ── Factory ───────────────────────────────────────────────────────────────

    public static BundleOrder Create(
        Guid userId,
        CollarModel collarModel,
        string paymentReference,
        decimal amountCrc,
        string shippingFullName,
        string shippingAddress,
        string shippingCanton,
        string shippingPhone,
        string? deliveryNotes) => new()
        {
            Id = Guid.CreateVersion7(),
            UserId = userId,
            CollarModel = collarModel,
            Status = BundleOrderStatus.PendingPayment,
            PaymentReference = paymentReference,
            AmountCrc = amountCrc,
            ShippingFullName = shippingFullName.Trim(),
            ShippingAddress = shippingAddress.Trim(),
            ShippingCanton = shippingCanton.Trim(),
            ShippingPhone = shippingPhone.Trim(),
            DeliveryNotes = deliveryNotes?.Trim(),
            CreatedAt = DateTimeOffset.UtcNow,
        };

    // ── Behaviour ─────────────────────────────────────────────────────────────

    public void ReportPaymentSent() => PaymentReportedByUser = true;

    public void ConfirmPayment(Guid subscriptionId)
    {
        Status = BundleOrderStatus.Paid;
        ActivatedSubscriptionId = subscriptionId;
        PaidAt = DateTimeOffset.UtcNow;
    }

    public void MarkSourcing(string? adminNotes = null)
    {
        Status = BundleOrderStatus.Sourcing;
        SourcedAt = DateTimeOffset.UtcNow;
        if (adminNotes is not null) AdminNotes = adminNotes;
    }

    public void MarkShipped(string trackingNumber, string? adminNotes = null)
    {
        Status = BundleOrderStatus.Shipped;
        TrackingNumber = trackingNumber.Trim();
        ShippedAt = DateTimeOffset.UtcNow;
        if (adminNotes is not null) AdminNotes = adminNotes;
    }

    public void MarkDelivered()
    {
        Status = BundleOrderStatus.Delivered;
        DeliveredAt = DateTimeOffset.UtcNow;
    }

    public void Cancel(string? adminNotes = null)
    {
        Status = BundleOrderStatus.Cancelled;
        CancelledAt = DateTimeOffset.UtcNow;
        if (adminNotes is not null) AdminNotes = adminNotes;
    }

    // ── Computed ──────────────────────────────────────────────────────────────

    public bool IsActive => Status is not (BundleOrderStatus.Delivered or BundleOrderStatus.Cancelled);
    public bool CanBeCancelledByUser => Status == BundleOrderStatus.PendingPayment;
}
