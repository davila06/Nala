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
        string? customerNote) => new()
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
            Quantity = quantity,
            CustomerNote = customerNote?.Trim(),
            Status = ProviderBookingStatus.Requested,
            CreatedAt = DateTimeOffset.UtcNow,
        };

    public void Confirm()
    {
        if (Status != ProviderBookingStatus.Requested)
            throw new InvalidOperationException("Solo una reserva solicitada puede confirmarse.");

        Status = ProviderBookingStatus.Confirmed;
        ConfirmedAt = DateTimeOffset.UtcNow;
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
        if (Status != ProviderBookingStatus.Requested)
            throw new InvalidOperationException("Solo una reserva solicitada puede vencer.");

        Status = ProviderBookingStatus.Expired;
        CancelledAt = DateTimeOffset.UtcNow;
    }

    public void CancelByCustomer(string reason) => Cancel(ProviderBookingStatus.CancelledByCustomer, reason);
    public void CancelByProvider(string reason) => Cancel(ProviderBookingStatus.CancelledByProvider, reason);

    private void Cancel(ProviderBookingStatus targetStatus, string reason)
    {
        if (Status is ProviderBookingStatus.Completed or ProviderBookingStatus.CancelledByCustomer or ProviderBookingStatus.CancelledByProvider or ProviderBookingStatus.NoShow or ProviderBookingStatus.Expired)
            throw new InvalidOperationException("No se puede cancelar una reserva finalizada.");
        if (string.IsNullOrWhiteSpace(reason))
            throw new InvalidOperationException("Se requiere un motivo de cancelacion.");

        Status = targetStatus;
        CancellationReason = reason.Trim();
        CancelledAt = DateTimeOffset.UtcNow;
    }
}