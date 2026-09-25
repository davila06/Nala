using FluentAssertions;
using NSubstitute;
using PawTrack.Application.Certificates.Commands.RescheduleVeterinarianAppointment;
using PawTrack.Application.Certificates.Interfaces;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Certificates;
using PawTrack.Domain.Clinics;

namespace PawTrack.UnitTests.Certificates.Commands;

public sealed class RescheduleVeterinarianAppointmentCommandHandlerTests
{
    [Fact]
    public async Task Handle_OverlappingAppointment_ReturnsFailure()
    {
        var clinicUserId = Guid.NewGuid();
        var clinic = Clinic.Create(
            clinicUserId,
            "Clinica Test",
            "VET-123",
            "San Jose",
            9.93m,
            -84.08m,
            "clinic@test.cr");
        clinic.Activate();

        var appointment = VeterinarianAppointment.Schedule(
            clinic.Id,
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTimeOffset.UtcNow.AddHours(2),
            TimeSpan.FromMinutes(30),
            clinicUserId);

        var clinics = Substitute.For<IClinicRepository>();
        clinics.GetByIdAsync(clinic.Id, Arg.Any<CancellationToken>()).Returns(clinic);

        var appointments = Substitute.For<IVeterinarianAppointmentRepository>();
        appointments.GetByIdAsync(appointment.Id, Arg.Any<CancellationToken>()).Returns(appointment);
        appointments.HasOverlapAsync(appointment.VeterinarianId, Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), appointment.Id, Arg.Any<CancellationToken>())
            .Returns(true);

        var unitOfWork = Substitute.For<IUnitOfWork>();
        var handler = new RescheduleVeterinarianAppointmentCommandHandler(
            clinics,
            appointments,
            Substitute.For<IVeterinarianScheduleBlockRepository>(),
            Substitute.For<IAuditLogRepository>(),
            unitOfWork);

        var result = await handler.Handle(
            new RescheduleVeterinarianAppointmentCommand(
                clinic.Id,
                clinicUserId,
                appointment.Id,
                DateTimeOffset.UtcNow.AddHours(3),
                30),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain("El veterinario ya tiene una cita en ese horario.");
        appointments.DidNotReceive().Update(Arg.Any<VeterinarianAppointment>());
    }
}
