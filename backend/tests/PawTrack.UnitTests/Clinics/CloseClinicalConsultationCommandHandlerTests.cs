using FluentAssertions;
using NSubstitute;
using PawTrack.Application.Clinics.Commands.CloseClinicalConsultation;
using PawTrack.Application.Clinics.Interfaces;
using PawTrack.Application.Certificates.Interfaces;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Audit;
using PawTrack.Domain.Certificates;
using PawTrack.Domain.Clinics;
using PawTrack.Domain.Medical;

namespace PawTrack.UnitTests.Clinics;

public sealed class CloseClinicalConsultationCommandHandlerTests
{
    [Fact]
    public async Task UnrelatedUser_CannotSignClinicConsultation()
    {
        var clinic = Clinic.Create(Guid.NewGuid(), "Clinica Test", "VET-123", "San Jose", 9.93m, -84.08m, "clinic@test.cr");
        var appointment = VeterinarianAppointment.Schedule(clinic.Id, Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow.AddHours(-1), TimeSpan.FromMinutes(30));
        appointment.Confirm(); appointment.CheckIn(); appointment.StartConsultation();
        var consultation = ClinicalConsultation.Create(clinic.Id, appointment.Id, appointment.PetId,
            appointment.VeterinarianId, Guid.NewGuid(), clinic.UserId, "Control", "S", "O", "A", "P",
            null, null, null, null, null, null, null, "Dx", "Tx", "Resumen");
        var clinics = Substitute.For<IClinicRepository>();
        clinics.GetByIdAsync(clinic.Id, Arg.Any<CancellationToken>()).Returns(clinic);
        var consultations = Substitute.For<IClinicalConsultationRepository>();
        consultations.GetByIdAsync(consultation.Id, Arg.Any<CancellationToken>()).Returns(consultation);
        var appointments = Substitute.For<IVeterinarianAppointmentRepository>();
        appointments.GetByIdAsync(appointment.Id, Arg.Any<CancellationToken>()).Returns(appointment);
        var medical = Substitute.For<IMedicalRepository>();
        var handler = new CloseClinicalConsultationCommandHandler(consultations, appointments, medical,
            Substitute.For<IClinicInventoryRepository>(), Substitute.For<IAuditLogRepository>(), Substitute.For<IUnitOfWork>(),
            clinics, Substitute.For<IClinicStaffAccessRepository>(), Substitute.For<IClinicVeterinarianRepository>());

        var result = await handler.Handle(new CloseClinicalConsultationCommand(clinic.Id, Guid.NewGuid(), consultation.Id, "Dra. Ana"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        await medical.DidNotReceive().AddAsync(Arg.Any<MedicalRecord>(), Arg.Any<CancellationToken>());
        appointment.Status.Should().Be(VeterinarianAppointmentStatus.InConsultation);
    }

    [Fact]
    public async Task Handle_DraftConsultation_ClosesSignsCreatesMedicalRecordAndCompletesAppointment()
    {
        var clinicUserId = Guid.NewGuid();
        var clinic = Clinic.Create(clinicUserId, "Clinica Test", "VET-123", "San Jose", 9.93m, -84.08m, "clinic@test.cr");
        var clinicId = clinic.Id;
        var appointment = VeterinarianAppointment.Schedule(clinicId, Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow.AddHours(-1), TimeSpan.FromMinutes(30), clinicUserId);
        appointment.Confirm();
        appointment.CheckIn();
        appointment.StartConsultation();
        var consultation = ClinicalConsultation.Create(
            clinicId,
            appointment.Id,
            appointment.PetId,
            appointment.VeterinarianId,
            Guid.NewGuid(),
            clinicUserId,
            "Control",
            "S",
            "O",
            "A",
            "P",
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            "Dx",
            "Tx",
            "Resumen");

        var consultations = Substitute.For<IClinicalConsultationRepository>();
        consultations.GetByIdAsync(consultation.Id, Arg.Any<CancellationToken>()).Returns(consultation);
        var appointments = Substitute.For<IVeterinarianAppointmentRepository>();
        appointments.GetByIdAsync(appointment.Id, Arg.Any<CancellationToken>()).Returns(appointment);
        var medical = Substitute.For<IMedicalRepository>();
        var audit = Substitute.For<IAuditLogRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var clinics = Substitute.For<IClinicRepository>();
        clinics.GetByIdAsync(clinicId, Arg.Any<CancellationToken>()).Returns(clinic);

        var handler = new CloseClinicalConsultationCommandHandler(
            consultations,
            appointments,
            medical,
            Substitute.For<IClinicInventoryRepository>(),
            audit,
            unitOfWork,
            clinics,
            Substitute.For<IClinicStaffAccessRepository>(),
            Substitute.For<IClinicVeterinarianRepository>());

        var result = await handler.Handle(
            new CloseClinicalConsultationCommand(clinicId, clinicUserId, consultation.Id, "Dra. Ana Mora"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        consultation.Status.Should().Be(ClinicalConsultationStatus.Closed);
        appointment.Status.Should().Be(VeterinarianAppointmentStatus.Completed);
        await medical.Received(1).AddAsync(Arg.Is<MedicalRecord>(record => record.PetId == consultation.PetId), Arg.Any<CancellationToken>());
        await audit.Received(1).AddAsync(
            Arg.Is<AuditLogEntry>(entry => entry.Action == AuditAction.ClinicalConsultationClosed),
            Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
