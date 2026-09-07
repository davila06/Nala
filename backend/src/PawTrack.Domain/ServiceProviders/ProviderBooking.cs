namespace PawTrack.Domain.ServiceProviders;

public sealed class ProviderBooking
{
    private ProviderBooking() { }

    public Guid Id { get; private set; }
    public Guid ServiceProviderId { get; private set; }
    public Guid ProviderServiceId { get; private set; }
    public Guid CustomerUserId { get; private set; }
    public Guid PetId { get; private set; }
    public string ServiceName { get; private set; } = string.Empty;
    public DateTimeOffset StartsAt { get; private set; }
    public DateTimeOffset EndsAt { get; private set; }
    public decimal PriceCrc { get; private set; }
    public decimal SubtotalCrc { get; private set; }
    public decimal TaxCrc { get; private set; }
    public decimal PlatformFeeCrc { get; private set; }
    public decimal TotalCrc { get; private set; }
    public string CancellationPolicySnapshot { get; private set; } = "Standard";
    public int FreeCancellationHours { get; private set; } = 48;
    public decimal CustomerRefundPercentage { get; private set; } = 100;
    public decimal ProviderCancellationRefundPercentage { get; private set; } = 100;
    public int Quantity { get; private set; }
    public string? CustomerNote { get; private set; }
    public ProviderBookingStatus Status { get; private set; }
    public string? CancellationReason { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? ConfirmedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public DateTimeOffset? CancelledAt { get; private set; }

    public static ProviderBooking Request(
        Guid serviceProviderId,
        Guid providerServiceId,
        Guid customerUserId,
        Guid petId,
        string serviceName,
        DateTimeOffset startsAt,
        int durationMinutes,
        decimal priceCrc,
        int quantity,
        string? customerNote,
        decimal taxCrc = 0,
        decimal platformFeeCrc = 0,
        string cancellationPolicySnapshot = "Standard",
        int freeCancellationHours = 48,
        decimal customerRefundPercentage = 100,
        decimal providerCancellationRefundPercentage = 100)
    {
        var subtotal = priceCrc * quantity;
        if (taxCrc < 0 || platformFeeCrc < 0) throw new ArgumentOutOfRangeException(nameof(taxCrc));
        if (string.IsNullOrWhiteSpace(cancellationPolicySnapshot)) throw new ArgumentException("La política de cancelación es requerida.", nameof(cancellationPolicySnapshot));
        if (freeCancellationHours < 0) throw new ArgumentOutOfRangeException(nameof(freeCancellationHours));
        if (customerRefundPercentage is < 0 or > 100) throw new ArgumentOutOfRangeException(nameof(customerRefundPercentage));
        if (providerCancellationRefundPercentage is < 0 or > 100) throw new ArgumentOutOfRangeException(nameof(providerCancellationRefundPercentage));
        return new ProviderBooking
        {
            Id = Guid.CreateVersion7(),
            ServiceProviderId = serviceProviderId,
            ProviderServiceId = providerServiceId,
            CustomerUserId = customerUserId,
            PetId = petId,
            ServiceName = serviceName.Trim(),
            StartsAt = startsAt,
            EndsAt = startsAt.AddMinutes(durationMinutes),
            PriceCrc = priceCrc,
            SubtotalCrc = subtotal,
            TaxCrc = taxCrc,
            PlatformFeeCrc = platformFeeCrc,
            TotalCrc = subtotal + taxCrc + platformFeeCrc,
            CancellationPolicySnapshot = cancellationPolicySnapshot.Trim(),
            FreeCancellationHours = freeCancellationHours,
            CustomerRefundPercentage = customerRefundPercentage,
            ProviderCancellationRefundPercentage = providerCancellationRefundPercentage,
            Quantity = quantity,
            CustomerNote = customerNote?.Trim(),
            Status = ProviderBookingStatus.Requested,
            CreatedAt = DateTimeOffset.UtcNow,
        };
    }

    public void Confirm()
    {
        if (Status is not (ProviderBookingStatus.Requested or ProviderBookingStatus.AwaitingPayment))
            throw new InvalidOperationException("Solo una reserva solicitada o pendiente de pago puede confirmarse.");

        Status = ProviderBookingStatus.Confirmed;
        ConfirmedAt = DateTimeOffset.UtcNow;
    }

    public void MarkAwaitingPayment()
    {
        if (Status is not ProviderBookingStatus.Requested)
            throw new InvalidOperationException("Solo una reserva solicitada puede quedar pendiente de pago.");

        Status = ProviderBookingStatus.AwaitingPayment;
    }

    public void MarkDisputed(string reason)
    {
        if (Status is not (ProviderBookingStatus.Requested or ProviderBookingStatus.AwaitingPayment or ProviderBookingStatus.Confirmed or ProviderBookingStatus.InProgress))
            throw new InvalidOperationException("La reserva no puede pasar a disputada en su estado actual.");
        if (string.IsNullOrWhiteSpace(reason))
            throw new InvalidOperationException("Se requiere un motivo de disputa.");

        Status = ProviderBookingStatus.Disputed;
        CancellationReason = reason.Trim();
        CancelledAt = DateTimeOffset.UtcNow;
    }

    public void IssueRefund(string reason)
    {
        if (Status is not (ProviderBookingStatus.Confirmed or ProviderBookingStatus.InProgress or ProviderBookingStatus.Completed or ProviderBookingStatus.Disputed or ProviderBookingStatus.CancelledByCustomer or ProviderBookingStatus.CancelledByProvider or ProviderBookingStatus.NoShow or ProviderBookingStatus.Expired or ProviderBookingStatus.AwaitingPayment))
            throw new InvalidOperationException("La reserva no puede reembolsarse en su estado actual.");
        if (string.IsNullOrWhiteSpace(reason))
            throw new InvalidOperationException("Se requiere un motivo de reembolso.");

        Status = ProviderBookingStatus.Refunded;
        CancellationReason = reason.Trim();
        CancelledAt = DateTimeOffset.UtcNow;
    }

    public void Start()
    {
        if (Status != ProviderBookingStatus.Confirmed)
            throw new InvalidOperationException("Solo una reserva confirmada puede iniciarse.");

        Status = ProviderBookingStatus.InProgress;
    }

    public void Complete()
    {
        if (Status != ProviderBookingStatus.InProgress)
            throw new InvalidOperationException("Solo una reserva en curso puede completarse.");

        Status = ProviderBookingStatus.Completed;
        CompletedAt = DateTimeOffset.UtcNow;
    }

    public void MarkNoShow()
    {
        if (Status != ProviderBookingStatus.Confirmed)
            throw new InvalidOperationException("Solo una reserva confirmada puede marcarse como inasistencia.");

        Status = ProviderBookingStatus.NoShow;
    }

    public void Reschedule(DateTimeOffset startsAt, int durationMinutes)
    {
        if (Status is not (ProviderBookingStatus.Requested or ProviderBookingStatus.Confirmed))
            throw new InvalidOperationException("Solo una reserva solicitada o confirmada puede reprogramarse.");
        if (startsAt <= DateTimeOffset.UtcNow)
            throw new InvalidOperationException("La nueva fecha debe estar en el futuro.");

        StartsAt = startsAt;
        EndsAt = startsAt.AddMinutes(durationMinutes);
        if (Status == ProviderBookingStatus.Confirmed)
        {
            Status = ProviderBookingStatus.Requested;
            ConfirmedAt = null;
        }
    }

    public void Expire()
    {
        if (Status is not (ProviderBookingStatus.Requested or ProviderBookingStatus.AwaitingPayment))
            throw new InvalidOperationException("Solo una reserva solicitada o pendiente de pago puede vencer.");

        Status = ProviderBookingStatus.Expired;
        CancelledAt = DateTimeOffset.UtcNow;
    }

    public void CancelByCustomer(string reason) => CancelByCustomer(reason, DateTimeOffset.UtcNow);

    public void CancelByCustomer(string reason, DateTimeOffset now)
    {
        if (!new ProviderCancellationPolicy(
                CancellationPolicySnapshot,
                FreeCancellationHours,
                CustomerRefundPercentage,
                ProviderCancellationRefundPercentage)
            .IsCustomerCancellationFree(StartsAt, now))
            throw new InvalidOperationException("La cancelación está fuera de la ventana gratuita de la política capturada.");
        Cancel(ProviderBookingStatus.CancelledByCustomer, reason);
    }
    public void CancelByProvider(string reason) => Cancel(ProviderBookingStatus.CancelledByProvider, reason);

    private void Cancel(ProviderBookingStatus targetStatus, string reason)
    {
        if (Status is ProviderBookingStatus.Completed or ProviderBookingStatus.CancelledByCustomer or ProviderBookingStatus.CancelledByProvider or ProviderBookingStatus.NoShow or ProviderBookingStatus.Expired or ProviderBookingStatus.Disputed or ProviderBookingStatus.Refunded)
            throw new InvalidOperationException("No se puede cancelar una reserva finalizada.");
        if (string.IsNullOrWhiteSpace(reason))
            throw new InvalidOperationException("Se requiere un motivo de cancelacion.");

        Status = targetStatus;
        CancellationReason = reason.Trim();
        CancelledAt = DateTimeOffset.UtcNow;
    }
}