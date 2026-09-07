namespace PawTrack.Domain.ServiceProviders;

public sealed class ServiceAvailabilityBlock
{
    private ServiceAvailabilityBlock() { }

    public Guid Id { get; private set; }
    public Guid ProviderServiceId { get; private set; }
    public DateTimeOffset StartsAt { get; private set; }
    public DateTimeOffset EndsAt { get; private set; }
    public string Reason { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public static ServiceAvailabilityBlock Create(
        Guid providerServiceId,
        DateTimeOffset startsAt,
        DateTimeOffset endsAt,
        string reason)
    {
        if (endsAt <= startsAt) throw new InvalidOperationException("El fin del bloqueo debe ser posterior al inicio.");
        if (string.IsNullOrWhiteSpace(reason)) throw new InvalidOperationException("Se requiere un motivo de bloqueo.");
        return new ServiceAvailabilityBlock
        {
            Id = Guid.CreateVersion7(),
            ProviderServiceId = providerServiceId,
            StartsAt = startsAt,
            EndsAt = endsAt,
            Reason = reason.Trim(),
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
        };
    }

    public bool Blocks(DateTimeOffset startsAt, DateTimeOffset endsAt) => IsActive && StartsAt < endsAt && startsAt < EndsAt;
    public void Deactivate() => IsActive = false;
}