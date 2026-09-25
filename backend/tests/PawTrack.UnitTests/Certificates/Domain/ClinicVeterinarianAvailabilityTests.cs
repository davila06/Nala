using FluentAssertions;
using PawTrack.Domain.Certificates;

namespace PawTrack.UnitTests.Certificates.Domain;

public sealed class ClinicVeterinarianAvailabilityTests
{
    [Fact]
    public void Availability_RejectsOverlappingAppointments()
    {
        var veterinarianId = Guid.NewGuid();
        var first = VeterinarianAppointment.Schedule(
            Guid.NewGuid(), veterinarianId, Guid.NewGuid(),
            new DateTimeOffset(2026, 9, 10, 10, 0, 0, TimeSpan.FromHours(-6)),
            TimeSpan.FromMinutes(30));
        var overlap = VeterinarianAppointment.Schedule(
            Guid.NewGuid(), veterinarianId, Guid.NewGuid(),
            first.StartsAt.AddMinutes(15), TimeSpan.FromMinutes(30));

        VeterinarianAppointment.Overlaps(first, overlap).Should().BeTrue();
    }

    [Fact]
    public void AppointmentLifecycle_AllowsDailyClinicWorkflow()
    {
        var appointment = VeterinarianAppointment.Schedule(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            new DateTimeOffset(2026, 9, 10, 10, 0, 0, TimeSpan.FromHours(-6)),
            TimeSpan.FromMinutes(30));

        appointment.Confirm();
        appointment.CheckIn();
        appointment.StartConsultation();
        appointment.Complete();

        appointment.Status.Should().Be(VeterinarianAppointmentStatus.Completed);
    }

    [Fact]
    public void AppointmentLifecycle_RejectsCompletingBeforeConsultationStarts()
    {
        var appointment = VeterinarianAppointment.Schedule(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            new DateTimeOffset(2026, 9, 10, 10, 0, 0, TimeSpan.FromHours(-6)),
            TimeSpan.FromMinutes(30));

        var act = () => appointment.Complete();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Only in-consultation appointments can be completed.");
    }

    [Fact]
    public void Reschedule_OpenAppointment_UpdatesTimeWindow()
    {
        var appointment = VeterinarianAppointment.Schedule(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            new DateTimeOffset(2026, 9, 10, 10, 0, 0, TimeSpan.FromHours(-6)),
            TimeSpan.FromMinutes(30));

        var newStart = appointment.StartsAt.AddHours(2);
        appointment.Reschedule(newStart, TimeSpan.FromMinutes(45));

        appointment.StartsAt.Should().Be(newStart);
        appointment.EndsAt.Should().Be(newStart.AddMinutes(45));
    }
}
