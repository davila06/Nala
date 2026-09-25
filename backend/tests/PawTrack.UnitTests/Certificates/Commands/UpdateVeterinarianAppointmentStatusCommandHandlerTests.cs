using FluentAssertions;
using NSubstitute;
using PawTrack.Application.Certificates.Commands.UpdateVeterinarianAppointmentStatus;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Audit;
using PawTrack.Domain.Certificates;
using PawTrack.Domain.Clinics;

namespace PawTrack.UnitTests.Certificates.Commands;

public sealed class UpdateVeterinarianAppointmentStatusCommandHandlerTests
{
    [Fact]
    public async Task Handle_ClinicOwnedAppointment_ConfirmsAndPersists()
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

        var auditLog = Substitute.For<IAuditLogRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var handler = new UpdateVeterinarianAppointmentStatusCommandHandler(
            clinics,
            appointments,
            auditLog,
            unitOfWork);

        var result = await handler.Handle(
            new UpdateVeterinarianAppointmentStatusCommand(
                clinic.Id,
                clinicUserId,
                appointment.Id,
                VeterinarianAppointmentStatus.Confirmed),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        appointment.Status.Should().Be(VeterinarianAppointmentStatus.Confirmed);
        appointments.Received(1).Update(appointment);
        await auditLog.Received(1).AddAsync(
            Arg.Is<AuditLogEntry>(entry =>
                entry.Action == AuditAction.ClinicAppointmentStatusChanged &&
                entry.EntityType == "VeterinarianAppointment" &&
                entry.EntityId == appointment.Id.ToString()),
            Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
