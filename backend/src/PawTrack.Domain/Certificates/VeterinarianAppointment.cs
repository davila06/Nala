namespace PawTrack.Domain.Certificates;

public enum VeterinarianAppointmentStatus
{
    Scheduled,
    Confirmed,
    CheckedIn,
    InConsultation,
    Completed,
    NoShow,
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
        && IsBlocking(left.Status)
        && IsBlocking(right.Status)
        && left.StartsAt < right.EndsAt
        && right.StartsAt < left.EndsAt;

    public void Confirm()
    {
        RequireStatus(VeterinarianAppointmentStatus.Scheduled, "Only scheduled appointments can be confirmed.");
        Status = VeterinarianAppointmentStatus.Confirmed;
    }

    public void CheckIn()
    {
        RequireStatus(VeterinarianAppointmentStatus.Confirmed, "Only confirmed appointments can be checked in.");
        Status = VeterinarianAppointmentStatus.CheckedIn;
    }

    public void StartConsultation()
    {
        RequireStatus(VeterinarianAppointmentStatus.CheckedIn, "Only checked-in appointments can start consultation.");
        Status = VeterinarianAppointmentStatus.InConsultation;
    }

    public void Complete()
    {
        RequireStatus(VeterinarianAppointmentStatus.InConsultation, "Only in-consultation appointments can be completed.");
        Status = VeterinarianAppointmentStatus.Completed;
    }

    public void MarkNoShow()
    {
        if (Status is not (VeterinarianAppointmentStatus.Scheduled or VeterinarianAppointmentStatus.Confirmed))
            throw new InvalidOperationException("Only scheduled or confirmed appointments can be marked as no-show.");
        Status = VeterinarianAppointmentStatus.NoShow;
    }

    public void Cancel()
    {
        if (Status is VeterinarianAppointmentStatus.Completed or VeterinarianAppointmentStatus.NoShow or VeterinarianAppointmentStatus.Cancelled)
            throw new InvalidOperationException("Only open appointments can be cancelled.");
        Status = VeterinarianAppointmentStatus.Cancelled;
    }

    public void Reschedule(DateTimeOffset startsAt, TimeSpan duration)
    {
        if (!IsBlocking(Status))
            throw new InvalidOperationException("Only open appointments can be rescheduled.");
        if (duration <= TimeSpan.Zero || duration > TimeSpan.FromHours(8))
            throw new ArgumentOutOfRangeException(nameof(duration));

        StartsAt = startsAt;
        EndsAt = startsAt.Add(duration);
    }

    private static bool IsBlocking(VeterinarianAppointmentStatus status) =>
        status is VeterinarianAppointmentStatus.Scheduled
            or VeterinarianAppointmentStatus.Confirmed
            or VeterinarianAppointmentStatus.CheckedIn
            or VeterinarianAppointmentStatus.InConsultation;

    private void RequireStatus(VeterinarianAppointmentStatus expected, string message)
    {
        if (Status != expected) throw new InvalidOperationException(message);
    }
}
