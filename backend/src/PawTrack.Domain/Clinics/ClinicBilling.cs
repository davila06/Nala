namespace PawTrack.Domain.Clinics;

public enum ClinicSaleStatus
{
    Open,
    PartiallyPaid,
    Paid,
    Voided,
}

public enum ClinicSaleLineType
{
    Service,
    InventoryItem,
    Other,
}

public enum ClinicPaymentMethod
{
    Cash,
    Card,
    Sinpe,
    Transfer,
    InternalCredit,
}

public sealed class ClinicSale
{
    private readonly List<ClinicSaleLine> _lines = [];
    private readonly List<ClinicSalePayment> _payments = [];
    private readonly List<ClinicSaleRefund> _refunds = [];

    private ClinicSale() { }

    public Guid Id { get; private set; }
    public Guid ClinicId { get; private set; }
    public Guid? AppointmentId { get; private set; }
    public Guid? ConsultationId { get; private set; }
    public Guid? PetId { get; private set; }
    public Guid CreatedByUserId { get; private set; }
    public string ReceiptNumber { get; private set; } = string.Empty;
    public ClinicSaleStatus Status { get; private set; }
    public decimal DiscountCrc { get; private set; }
    public string? DiscountReason { get; private set; }
    public Guid? DiscountApprovedByUserId { get; private set; }
    public string? VoidReason { get; private set; }
    public Guid? VoidedByUserId { get; private set; }
    public DateTimeOffset? VoidedAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public IReadOnlyList<ClinicSaleLine> Lines => _lines.AsReadOnly();
    public IReadOnlyList<ClinicSalePayment> Payments => _payments.AsReadOnly();
    public IReadOnlyList<ClinicSaleRefund> Refunds => _refunds.AsReadOnly();
    public decimal SubtotalCrc => _lines.Sum(line => line.LineTotalCrc);
    public decimal TotalCrc => Math.Max(0m, SubtotalCrc - DiscountCrc);
    public decimal PaidCrc => _payments.Sum(payment => payment.AmountCrc) - _refunds.Sum(refund => refund.AmountCrc);
    public decimal BalanceCrc => Math.Max(0m, TotalCrc - PaidCrc);

    public static ClinicSale Create(
        Guid clinicId,
        Guid createdByUserId,
        Guid? appointmentId,
        Guid? consultationId,
        Guid? petId,
        string receiptNumber)
    {
        if (clinicId == Guid.Empty) throw new ArgumentException("ClinicId is required.", nameof(clinicId));
        if (createdByUserId == Guid.Empty) throw new ArgumentException("CreatedByUserId is required.", nameof(createdByUserId));
        if (string.IsNullOrWhiteSpace(receiptNumber)) throw new ArgumentException("Receipt number is required.", nameof(receiptNumber));

        return new ClinicSale
        {
            Id = Guid.CreateVersion7(),
            ClinicId = clinicId,
            CreatedByUserId = createdByUserId,
            AppointmentId = appointmentId,
            ConsultationId = consultationId,
            PetId = petId,
            ReceiptNumber = receiptNumber.Trim().ToUpperInvariant(),
            Status = ClinicSaleStatus.Open,
            CreatedAt = DateTimeOffset.UtcNow,
        };
    }

    public void AddLine(
        string description,
        ClinicSaleLineType type,
        int quantity,
        decimal unitPriceCrc,
        Guid? inventoryItemId,
        Guid? inventoryLotId)
    {
        EnsureOpen();
        _lines.Add(ClinicSaleLine.Create(Id, description, type, quantity, unitPriceCrc, inventoryItemId, inventoryLotId));
        RecalculateStatus();
    }

    public void ApplyDiscount(decimal amountCrc, string reason, Guid approvedByUserId)
    {
        EnsureOpen();
        if (amountCrc < 0 || amountCrc > SubtotalCrc) throw new ArgumentOutOfRangeException(nameof(amountCrc));
        if (amountCrc > 0 && string.IsNullOrWhiteSpace(reason)) throw new ArgumentException("Discount reason is required.", nameof(reason));
        if (amountCrc > 0 && approvedByUserId == Guid.Empty) throw new ArgumentException("Approver is required.", nameof(approvedByUserId));

        DiscountCrc = amountCrc;
        DiscountReason = amountCrc == 0 ? null : reason.Trim();
        DiscountApprovedByUserId = amountCrc == 0 ? null : approvedByUserId;
        RecalculateStatus();
    }

    public ClinicSalePayment RecordPayment(decimal amountCrc, ClinicPaymentMethod method, string? reference, Guid receivedByUserId)
    {
        EnsureOpen();
        if (amountCrc <= 0) throw new ArgumentOutOfRangeException(nameof(amountCrc));
        if (amountCrc > BalanceCrc) throw new InvalidOperationException("El pago excede el saldo pendiente.");
        var payment = ClinicSalePayment.Create(Id, ClinicId, amountCrc, method, reference, receivedByUserId);
        _payments.Add(payment);
        RecalculateStatus();
        return payment;
    }

    public ClinicSaleRefund RecordRefund(Guid paymentId, decimal amountCrc, string reason, string evidenceReference, Guid refundedByUserId)
    {
        if (Status == ClinicSaleStatus.Voided) throw new InvalidOperationException("La venta ya fue anulada.");
        var payment = _payments.SingleOrDefault(item => item.Id == paymentId)
            ?? throw new InvalidOperationException("El pago no pertenece a la venta.");
        if (amountCrc <= 0) throw new ArgumentOutOfRangeException(nameof(amountCrc));
        if (amountCrc > payment.AmountCrc - _refunds.Where(item => item.PaymentId == paymentId).Sum(item => item.AmountCrc))
            throw new InvalidOperationException("La devolución excede el pago pendiente.");
        if (string.IsNullOrWhiteSpace(reason) || string.IsNullOrWhiteSpace(evidenceReference))
            throw new ArgumentException("Motivo y comprobante de devolución son obligatorios.");
        if (refundedByUserId == Guid.Empty) throw new ArgumentException("Responsable requerido.", nameof(refundedByUserId));
        if (_refunds.Any(item => item.EvidenceReference == evidenceReference.Trim()))
            throw new InvalidOperationException("Comprobante de devolución duplicado.");
        var refund = ClinicSaleRefund.Create(Id, ClinicId, payment.Id, payment.Method, amountCrc, reason, evidenceReference, refundedByUserId);
        _refunds.Add(refund);
        RecalculateStatus();
        return refund;
    }

    public void Void(string reason, Guid voidedByUserId)
    {
        if (Status == ClinicSaleStatus.Voided) throw new InvalidOperationException("La venta ya fue anulada.");
        if (PaidCrc > 0) throw new InvalidOperationException("Se deben registrar todas las devoluciones antes de anular.");
        if (string.IsNullOrWhiteSpace(reason)) throw new ArgumentException("Void reason is required.", nameof(reason));
        if (voidedByUserId == Guid.Empty) throw new ArgumentException("VoidedByUserId is required.", nameof(voidedByUserId));

        Status = ClinicSaleStatus.Voided;
        VoidReason = reason.Trim();
        VoidedByUserId = voidedByUserId;
        VoidedAt = DateTimeOffset.UtcNow;
    }

    private void EnsureOpen()
    {
        if (Status == ClinicSaleStatus.Voided) throw new InvalidOperationException("La venta está anulada.");
        if (Status == ClinicSaleStatus.Paid) throw new InvalidOperationException("La venta ya está pagada.");
    }

    private void RecalculateStatus()
    {
        if (Status == ClinicSaleStatus.Voided) return;
        Status = PaidCrc <= 0 ? ClinicSaleStatus.Open : BalanceCrc == 0 ? ClinicSaleStatus.Paid : ClinicSaleStatus.PartiallyPaid;
    }
}

public sealed class ClinicSaleLine
{
    private ClinicSaleLine() { }

    public Guid Id { get; private set; }
    public Guid SaleId { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public ClinicSaleLineType Type { get; private set; }
    public int Quantity { get; private set; }
    public decimal UnitPriceCrc { get; private set; }
    public decimal LineTotalCrc { get; private set; }
    public Guid? InventoryItemId { get; private set; }
    public Guid? InventoryLotId { get; private set; }

    public static ClinicSaleLine Create(
        Guid saleId,
        string description,
        ClinicSaleLineType type,
        int quantity,
        decimal unitPriceCrc,
        Guid? inventoryItemId,
        Guid? inventoryLotId)
    {
        if (saleId == Guid.Empty) throw new ArgumentException("SaleId is required.", nameof(saleId));
        if (string.IsNullOrWhiteSpace(description)) throw new ArgumentException("Description is required.", nameof(description));
        if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity));
        if (unitPriceCrc < 0) throw new ArgumentOutOfRangeException(nameof(unitPriceCrc));

        return new ClinicSaleLine
        {
            Id = Guid.CreateVersion7(),
            SaleId = saleId,
            Description = description.Trim(),
            Type = type,
            Quantity = quantity,
            UnitPriceCrc = unitPriceCrc,
            LineTotalCrc = quantity * unitPriceCrc,
            InventoryItemId = inventoryItemId,
            InventoryLotId = inventoryLotId,
        };
    }
}

public sealed class ClinicSalePayment
{
    private ClinicSalePayment() { }

    public Guid Id { get; private set; }
    public Guid SaleId { get; private set; }
    public Guid ClinicId { get; private set; }
    public decimal AmountCrc { get; private set; }
    public ClinicPaymentMethod Method { get; private set; }
    public string? Reference { get; private set; }
    public Guid ReceivedByUserId { get; private set; }
    public DateTimeOffset ReceivedAt { get; private set; }

    public static ClinicSalePayment Create(
        Guid saleId,
        Guid clinicId,
        decimal amountCrc,
        ClinicPaymentMethod method,
        string? reference,
        Guid receivedByUserId)
    {
        if (amountCrc <= 0) throw new ArgumentOutOfRangeException(nameof(amountCrc));
        if (receivedByUserId == Guid.Empty) throw new ArgumentException("ReceivedByUserId is required.", nameof(receivedByUserId));
        return new ClinicSalePayment
        {
            Id = Guid.CreateVersion7(),
            SaleId = saleId,
            ClinicId = clinicId,
            AmountCrc = amountCrc,
            Method = method,
            Reference = string.IsNullOrWhiteSpace(reference) ? null : reference.Trim(),
            ReceivedByUserId = receivedByUserId,
            ReceivedAt = DateTimeOffset.UtcNow,
        };
    }
}

public sealed class ClinicSaleRefund
{
    private ClinicSaleRefund() { }

    public Guid Id { get; private set; }
    public Guid SaleId { get; private set; }
    public Guid ClinicId { get; private set; }
    public Guid PaymentId { get; private set; }
    public ClinicPaymentMethod Method { get; private set; }
    public decimal AmountCrc { get; private set; }
    public string Reason { get; private set; } = string.Empty;
    public string EvidenceReference { get; private set; } = string.Empty;
    public Guid RefundedByUserId { get; private set; }
    public DateTimeOffset RefundedAt { get; private set; }

    public static ClinicSaleRefund Create(Guid saleId, Guid clinicId, Guid paymentId, ClinicPaymentMethod method,
        decimal amountCrc, string reason, string evidenceReference, Guid refundedByUserId) => new()
        {
            Id = Guid.CreateVersion7(),
            SaleId = saleId,
            ClinicId = clinicId,
            PaymentId = paymentId,
            Method = method,
            AmountCrc = amountCrc,
            Reason = reason.Trim(),
            EvidenceReference = evidenceReference.Trim(),
            RefundedByUserId = refundedByUserId,
            RefundedAt = DateTimeOffset.UtcNow,
        };
}

public sealed class ClinicCashClose
{
    private readonly List<ClinicCashClosePaymentSnapshot> _paymentsByMethod = [];

    private ClinicCashClose() { }

    public Guid Id { get; private set; }
    public Guid ClinicId { get; private set; }
    public DateOnly BusinessDate { get; private set; }
    public Guid ClosedByUserId { get; private set; }
    public decimal TotalCrc { get; private set; }
    public DateTimeOffset ClosedAt { get; private set; }
    public IReadOnlyList<ClinicCashClosePaymentSnapshot> PaymentsByMethod => _paymentsByMethod.AsReadOnly();

    public static ClinicCashClose Create(
        Guid clinicId,
        DateOnly businessDate,
        Guid closedByUserId,
        IReadOnlyList<ClinicCashClosePaymentSnapshot> paymentsByMethod)
    {
        if (clinicId == Guid.Empty) throw new ArgumentException("ClinicId is required.", nameof(clinicId));
        if (closedByUserId == Guid.Empty) throw new ArgumentException("ClosedByUserId is required.", nameof(closedByUserId));

        var close = new ClinicCashClose
        {
            Id = Guid.CreateVersion7(),
            ClinicId = clinicId,
            BusinessDate = businessDate,
            ClosedByUserId = closedByUserId,
            TotalCrc = paymentsByMethod.Sum(payment => payment.AmountCrc),
            ClosedAt = DateTimeOffset.UtcNow,
        };
        close._paymentsByMethod.AddRange(paymentsByMethod);
        return close;
    }
}

public sealed record ClinicCashClosePaymentSnapshot(ClinicPaymentMethod Method, decimal AmountCrc);
