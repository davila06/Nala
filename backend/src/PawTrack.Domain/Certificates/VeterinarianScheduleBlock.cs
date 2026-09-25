namespace PawTrack.Domain.Certificates;

public sealed class VeterinarianScheduleBlock
{
    private VeterinarianScheduleBlock() { }

    public Guid Id { get; private set; }
    public Guid ClinicId { get; private set; }
    public Guid VeterinarianId { get; private set; }
    public DateTimeOffset StartsAt { get; private set; }
    public DateTimeOffset EndsAt { get; private set; }
    public string Reason { get; private set; } = string.Empty;
    public Guid CreatedByUserId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public static VeterinarianScheduleBlock Create(
        Guid clinicId,
        Guid veterinarianId,
        DateTimeOffset startsAt,
        DateTimeOffset endsAt,
        string reason,
        Guid createdByUserId)
    {
        if (clinicId == Guid.Empty) throw new ArgumentException("ClinicId is required.", nameof(clinicId));
        if (veterinarianId == Guid.Empty) throw new ArgumentException("VeterinarianId is required.", nameof(veterinarianId));
        if (endsAt <= startsAt) throw new ArgumentException("Block end must be after start.", nameof(endsAt));
        if ((endsAt - startsAt) > TimeSpan.FromDays(14)) throw new ArgumentOutOfRangeException(nameof(endsAt));

        return new VeterinarianScheduleBlock
        {
            Id = Guid.CreateVersion7(),
            ClinicId = clinicId,
            VeterinarianId = veterinarianId,
            StartsAt = startsAt,
            EndsAt = endsAt,
            Reason = string.IsNullOrWhiteSpace(reason) ? "Bloqueo de agenda" : reason.Trim(),
            CreatedByUserId = createdByUserId,
            CreatedAt = DateTimeOffset.UtcNow,
        };
    }

    public void UpdateWindow(DateTimeOffset startsAt, DateTimeOffset endsAt, string reason)
    {
        if (endsAt <= startsAt) throw new ArgumentException("Block end must be after start.", nameof(endsAt));
        if ((endsAt - startsAt) > TimeSpan.FromDays(14)) throw new ArgumentOutOfRangeException(nameof(endsAt));

        StartsAt = startsAt;
        EndsAt = endsAt;
        Reason = string.IsNullOrWhiteSpace(reason) ? "Bloqueo de agenda" : reason.Trim();
    }
}
