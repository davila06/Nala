namespace PawTrack.Domain.Certificates;

public enum VeterinarianAppointmentStatus
{
    Scheduled,
    Completed,
    Cancelled,
}

public sealed class VeterinarianAppointment
{
    private VeterinarianAppointment() { }

    public Guid Id { get; private set; }
    public Guid ClinicId { get; private set; }
    public Guid VeterinarianId { get; private set; }
    public Guid PetId { get; private set; }
    public DateTimeOffset StartsAt { get; private set; }
    public DateTimeOffset EndsAt { get; private set; }
    public VeterinarianAppointmentStatus Status { get; private set; }
    public Guid CreatedByUserId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public static VeterinarianAppointment Schedule(
        Guid clinicId,
        Guid veterinarianId,
        Guid petId,
        DateTimeOffset startsAt,
        TimeSpan duration,
        Guid createdByUserId = default)
    {
        if (duration <= TimeSpan.Zero || duration > TimeSpan.FromHours(8))
            throw new ArgumentOutOfRangeException(nameof(duration));
        return new VeterinarianAppointment
        {
            Id = Guid.CreateVersion7(),
            ClinicId = clinicId,
            VeterinarianId = veterinarianId,
            PetId = petId,
            StartsAt = startsAt,
            EndsAt = startsAt.Add(duration),
            Status = VeterinarianAppointmentStatus.Scheduled,
            CreatedByUserId = createdByUserId,
            CreatedAt = DateTimeOffset.UtcNow,
        };
    }

    public static bool Overlaps(VeterinarianAppointment left, VeterinarianAppointment right) =>
        left.VeterinarianId == right.VeterinarianId
        && left.Status == VeterinarianAppointmentStatus.Scheduled
        && right.Status == VeterinarianAppointmentStatus.Scheduled
        && left.StartsAt < right.EndsAt
        && right.StartsAt < left.EndsAt;
}