namespace PawTrack.Domain.ServiceProviders;

public sealed class ProviderPayment
{
    private ProviderPayment() { }

    public Guid Id { get; private set; }
    public Guid BookingId { get; private set; }
    public Guid CustomerUserId { get; private set; }
    public Guid ServiceProviderId { get; private set; }
    public decimal AmountCrc { get; private set; }
    public string Currency { get; private set; } = "CRC";
    public string PaymentReference { get; private set; } = string.Empty;
    public string IdempotencyKey { get; private set; } = string.Empty;
    public ProviderPaymentStatus Status { get; private set; }
    public string? ExternalReference { get; private set; }
    public string? FailureReason { get; private set; }
    public string? DisputeReason { get; private set; }
    public string? RefundReason { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? ReportedAt { get; private set; }
    public DateTimeOffset? ConfirmedAt { get; private set; }
    public DateTimeOffset? DisputedAt { get; private set; }
    public DateTimeOffset? RefundedAt { get; private set; }

    public static ProviderPayment Create(
        Guid bookingId,
        Guid customerUserId,
        Guid serviceProviderId,
        decimal amountCrc,
        string paymentReference,
        string idempotencyKey)
    {
        if (bookingId == Guid.Empty || customerUserId == Guid.Empty || serviceProviderId == Guid.Empty)
            throw new ArgumentException("Los identificadores del pago son requeridos.");
        if (amountCrc <= 0)
            throw new ArgumentOutOfRangeException(nameof(amountCrc));
        if (string.IsNullOrWhiteSpace(paymentReference))
            throw new ArgumentException("La referencia de pago es requerida.", nameof(paymentReference));
        if (string.IsNullOrWhiteSpace(idempotencyKey))
            throw new ArgumentException("La clave de idempotencia es requerida.", nameof(idempotencyKey));

        return new ProviderPayment
        {
            Id = Guid.CreateVersion7(),
            BookingId = bookingId,
            CustomerUserId = customerUserId,
            ServiceProviderId = serviceProviderId,
            AmountCrc = amountCrc,
            PaymentReference = paymentReference.Trim(),
            IdempotencyKey = idempotencyKey.Trim(),
            Status = ProviderPaymentStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow,
        };
    }

    public void ReportPayment()
    {
        if (Status != ProviderPaymentStatus.Pending)
            throw new InvalidOperationException("Solo un pago pendiente puede reportarse.");
        Status = ProviderPaymentStatus.Reported;
        ReportedAt = DateTimeOffset.UtcNow;
    }

    public void Confirm(string externalReference)
    {
        if (Status != ProviderPaymentStatus.Reported)
            throw new InvalidOperationException("Solo un pago reportado puede confirmarse.");
        if (string.IsNullOrWhiteSpace(externalReference))
            throw new ArgumentException("La referencia externa es requerida.", nameof(externalReference));
        Status = ProviderPaymentStatus.Confirmed;
        ExternalReference = externalReference.Trim();
        ConfirmedAt = DateTimeOffset.UtcNow;
    }

    public void MarkDisputed(string reason)
    {
        if (Status is not (ProviderPaymentStatus.Reported or ProviderPaymentStatus.Confirmed))
            throw new InvalidOperationException("El pago no puede pasar a disputa en su estado actual.");
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("El motivo de disputa es requerido.", nameof(reason));
        Status = ProviderPaymentStatus.Disputed;
        DisputeReason = reason.Trim();
        DisputedAt = DateTimeOffset.UtcNow;
    }

    public void Refund(string reason)
    {
        if (Status is not (ProviderPaymentStatus.Confirmed or ProviderPaymentStatus.Disputed))
            throw new InvalidOperationException("Solo un pago confirmado o disputado puede reembolsarse.");
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("El motivo de reembolso es requerido.", nameof(reason));
        Status = ProviderPaymentStatus.Refunded;
        RefundReason = reason.Trim();
        RefundedAt = DateTimeOffset.UtcNow;
    }

    public void Expire(string reason)
    {
        if (Status is not (ProviderPaymentStatus.Pending or ProviderPaymentStatus.Reported))
            throw new InvalidOperationException("Solo un pago pendiente o reportado puede vencer.");
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("El motivo de vencimiento es requerido.", nameof(reason));

        Status = ProviderPaymentStatus.Failed;
        FailureReason = reason.Trim();
    }
}