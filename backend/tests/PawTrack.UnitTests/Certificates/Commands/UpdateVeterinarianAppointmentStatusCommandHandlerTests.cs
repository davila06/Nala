using FluentAssertions;
using NSubstitute;
using PawTrack.Application.Certificates.Commands.UpdateVeterinarianAppointmentStatus;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Clinics.Interfaces;
using PawTrack.Domain.Audit;
using PawTrack.Domain.Certificates;
using PawTrack.Domain.Clinics;

namespace PawTrack.UnitTests.Certificates.Commands;

public sealed class UpdateVeterinarianAppointmentStatusCommandHandlerTests
{
    [Fact]
    public async Task Receptionist_ConfirmsOwnAppointmentButCannotMutateAnotherClinicsAppointment()
    {
        var clinic = Clinic.Create(Guid.NewGuid(), "Clinica Test", "VET-123", "San Jose", 9.93m, -84.08m, "clinic@test.cr");
        var receptionistId = Guid.NewGuid();
        var own = VeterinarianAppointment.Schedule(clinic.Id, Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow.AddHours(2), TimeSpan.FromMinutes(30));
        var foreign = VeterinarianAppointment.Schedule(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow.AddHours(3), TimeSpan.FromMinutes(30));
        var clinics = Substitute.For<IClinicRepository>();
        clinics.GetByIdAsync(clinic.Id, Arg.Any<CancellationToken>()).Returns(clinic);
        var appointments = Substitute.For<IVeterinarianAppointmentRepository>();
        appointments.GetByIdAsync(own.Id, Arg.Any<CancellationToken>()).Returns(own);
        appointments.GetByIdAsync(foreign.Id, Arg.Any<CancellationToken>()).Returns(foreign);
        var staff = Substitute.For<IClinicStaffAccessRepository>();
        staff.HasPermissionAsync(clinic.Id, receptionistId, ClinicStaffPermission.ManageAgenda, Arg.Any<CancellationToken>()).Returns(true);
        var handler = new UpdateVeterinarianAppointmentStatusCommandHandler(clinics, appointments,
            Substitute.For<IAuditLogRepository>(), Substitute.For<IUnitOfWork>(), staff);

        var confirmed = await handler.Handle(new UpdateVeterinarianAppointmentStatusCommand(clinic.Id, receptionistId, own.Id, VeterinarianAppointmentStatus.Confirmed), CancellationToken.None);
        var rejected = await handler.Handle(new UpdateVeterinarianAppointmentStatusCommand(clinic.Id, receptionistId, foreign.Id, VeterinarianAppointmentStatus.Confirmed), CancellationToken.None);

        confirmed.IsSuccess.Should().BeTrue();
        rejected.IsFailure.Should().BeTrue();
        foreign.Status.Should().Be(VeterinarianAppointmentStatus.Scheduled);
    }

    [Fact]
    public async Task Receptionist_CannotStartConsultation()
    {
        var clinic = Clinic.Create(Guid.NewGuid(), "Clinica Test", "VET-123", "San Jose", 9.93m, -84.08m, "clinic@test.cr");
        var receptionistId = Guid.NewGuid();
        var appointment = VeterinarianAppointment.Schedule(clinic.Id, Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow.AddHours(2), TimeSpan.FromMinutes(30));
        appointment.Confirm();
        appointment.CheckIn();
        var clinics = Substitute.For<IClinicRepository>();
        clinics.GetByIdAsync(clinic.Id, Arg.Any<CancellationToken>()).Returns(clinic);
        var appointments = Substitute.For<IVeterinarianAppointmentRepository>();
        appointments.GetByIdAsync(appointment.Id, Arg.Any<CancellationToken>()).Returns(appointment);
        var staff = Substitute.For<IClinicStaffAccessRepository>();
        staff.HasPermissionAsync(clinic.Id, receptionistId, ClinicStaffPermission.ManageAgenda, Arg.Any<CancellationToken>()).Returns(true);
        var handler = new UpdateVeterinarianAppointmentStatusCommandHandler(clinics, appointments,
            Substitute.For<IAuditLogRepository>(), Substitute.For<IUnitOfWork>(), staff);

        var result = await handler.Handle(new UpdateVeterinarianAppointmentStatusCommand(clinic.Id, receptionistId,
            appointment.Id, VeterinarianAppointmentStatus.InConsultation), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        appointment.Status.Should().Be(VeterinarianAppointmentStatus.CheckedIn);
    }

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
            unitOfWork,
            Substitute.For<IClinicStaffAccessRepository>());

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
