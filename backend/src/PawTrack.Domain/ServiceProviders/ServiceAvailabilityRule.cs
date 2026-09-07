namespace PawTrack.Domain.ServiceProviders;

public sealed class ServiceAvailabilityRule
{
    private ServiceAvailabilityRule() { }

    public Guid Id { get; private set; }
    public Guid ProviderServiceId { get; private set; }
    public DayOfWeek DayOfWeek { get; private set; }
    public TimeOnly StartsAtLocalTime { get; private set; }
    public TimeOnly EndsAtLocalTime { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public static ServiceAvailabilityRule Create(
        Guid providerServiceId,
        DayOfWeek dayOfWeek,
        TimeOnly startsAtLocalTime,
        TimeOnly endsAtLocalTime)
    {
        if (endsAtLocalTime <= startsAtLocalTime)
            throw new InvalidOperationException("La hora de fin debe ser posterior a la hora de inicio.");

        return new ServiceAvailabilityRule
        {
            Id = Guid.CreateVersion7(),
            ProviderServiceId = providerServiceId,
            DayOfWeek = dayOfWeek,
            StartsAtLocalTime = startsAtLocalTime,
            EndsAtLocalTime = endsAtLocalTime,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
        };
    }

    public bool Covers(DateTimeOffset startsAt, DateTimeOffset endsAt)
    {
        if (endsAt <= startsAt || startsAt.DayOfWeek != DayOfWeek || endsAt.DayOfWeek != DayOfWeek)
            return false;

        var startTime = TimeOnly.FromDateTime(startsAt.DateTime);
        var endTime = TimeOnly.FromDateTime(endsAt.DateTime);
        return IsActive && startTime >= StartsAtLocalTime && endTime <= EndsAtLocalTime;
    }

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
}