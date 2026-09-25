using FluentAssertions;
using NSubstitute;
using PawTrack.Application.Certificates.Interfaces;
using PawTrack.Application.Clinics.Commands.CreateClinicalConsultation;
using PawTrack.Application.Clinics.Interfaces;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Audit;
using PawTrack.Domain.Certificates;
using PawTrack.Domain.Clinics;
using PawTrack.Domain.Medical;
using PawTrack.Domain.Pets;

namespace PawTrack.UnitTests.Clinics;

public sealed class CreateClinicalConsultationCommandHandlerTests
{
    [Fact]
    public async Task Handle_AssignedStaffVeterinarianWithGrant_CreatesConsultation()
    {
        var clinic = Clinic.Create(Guid.NewGuid(), "Clinica Test", "VET-123", "San Jose", 9.93m, -84.08m, "clinic@test.cr");
        clinic.Activate();
        var userId = Guid.NewGuid();
        var veterinarian = ClinicVeterinarian.Create(clinic.Id, "Dra. Ana", "VET-999");
        var pet = Pet.Create(Guid.NewGuid(), "Nala", PetSpecies.Dog, null, null);
        var appointment = VeterinarianAppointment.Schedule(clinic.Id, veterinarian.Id, pet.Id, DateTimeOffset.UtcNow.AddHours(1), TimeSpan.FromMinutes(30));
        appointment.Confirm(); appointment.CheckIn(); appointment.StartConsultation();
        var clinics = Substitute.For<IClinicRepository>();
        clinics.GetByIdAsync(clinic.Id, Arg.Any<CancellationToken>()).Returns(clinic);
        var appointments = Substitute.For<IVeterinarianAppointmentRepository>();
        appointments.GetByIdAsync(appointment.Id, Arg.Any<CancellationToken>()).Returns(appointment);
        var veterinarians = Substitute.For<IClinicVeterinarianRepository>();
        veterinarians.GetByIdAsync(veterinarian.Id, Arg.Any<CancellationToken>()).Returns(veterinarian);
        var pets = Substitute.For<IPetRepository>();
        pets.GetByIdAsync(pet.Id, Arg.Any<CancellationToken>()).Returns(pet);
        var grants = Substitute.For<IClinicMedicalAccessGrantRepository>();
        var (grant, code) = ClinicMedicalAccessGrant.Generate(pet.Id, clinic.Id, pet.OwnerId, "Owner");
        grant.TryAccept(code).Should().BeTrue();
        grants.GetActiveGrantAsync(clinic.Id, pet.Id, Arg.Any<CancellationToken>()).Returns(grant);
        var staff = Substitute.For<IClinicStaffAccessRepository>();
        staff.GetAsync(clinic.Id, userId, Arg.Any<CancellationToken>())
            .Returns(ClinicStaffMembership.Grant(clinic.Id, userId, ClinicStaffRole.Veterinarian, clinic.UserId, veterinarian.Id));
        var consultations = Substitute.For<IClinicalConsultationRepository>();
        var handler = new CreateClinicalConsultationCommandHandler(clinics, appointments, veterinarians, pets,
            consultations, grants, Substitute.For<IClinicScanRepository>(), Substitute.For<IAuditLogRepository>(),
            Substitute.For<IUnitOfWork>(), staff);

        var result = await handler.Handle(new CreateClinicalConsultationCommand(clinic.Id, userId, appointment.Id,
            "Control", "S", "O", "A", "P", null, null, null, null, null, null, null, "Dx", "Tx", "Resumen"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await consultations.Received(1).AddAsync(Arg.Any<ClinicalConsultation>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AppointmentInConsultation_CreatesConsultationAndAudit()
    {
        var clinicUserId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var clinic = Clinic.Create(clinicUserId, "Clinica Test", "VET-123", "San Jose", 9.93m, -84.08m, "clinic@test.cr");
        clinic.Activate();
        var veterinarian = ClinicVeterinarian.Create(clinic.Id, "Dra. Ana", "VET-999");
        var pet = Pet.Create(ownerId, "Nala", PetSpecies.Dog, "Criolla", DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-3)));
        var appointment = VeterinarianAppointment.Schedule(clinic.Id, veterinarian.Id, pet.Id, DateTimeOffset.UtcNow.AddHours(1), TimeSpan.FromMinutes(30), clinicUserId);
        appointment.Confirm();
        appointment.CheckIn();
        appointment.StartConsultation();

        var clinics = Substitute.For<IClinicRepository>();
        clinics.GetByIdAsync(clinic.Id, Arg.Any<CancellationToken>()).Returns(clinic);
        var appointments = Substitute.For<IVeterinarianAppointmentRepository>();
        appointments.GetByIdAsync(appointment.Id, Arg.Any<CancellationToken>()).Returns(appointment);
        var veterinarians = Substitute.For<IClinicVeterinarianRepository>();
        veterinarians.GetByIdAsync(veterinarian.Id, Arg.Any<CancellationToken>()).Returns(veterinarian);
        var pets = Substitute.For<IPetRepository>();
        pets.GetByIdAsync(pet.Id, Arg.Any<CancellationToken>()).Returns(pet);
        var consultations = Substitute.For<IClinicalConsultationRepository>();
        var grants = Substitute.For<IClinicMedicalAccessGrantRepository>();
        var (grant, rawCode) = ClinicMedicalAccessGrant.Generate(pet.Id, clinic.Id, ownerId, "Owner");
        grant.TryAccept(rawCode).Should().BeTrue();
        grants.GetActiveGrantAsync(clinic.Id, pet.Id, Arg.Any<CancellationToken>()).Returns(grant);
        var scans = Substitute.For<IClinicScanRepository>();
        var audit = Substitute.For<IAuditLogRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();

        var handler = new CreateClinicalConsultationCommandHandler(
            clinics,
            appointments,
            veterinarians,
            pets,
            consultations,
            grants,
            scans,
            audit,
            unitOfWork,
            Substitute.For<IClinicStaffAccessRepository>());

        var result = await handler.Handle(new CreateClinicalConsultationCommand(
            clinic.Id,
            clinicUserId,
            appointment.Id,
            "Control",
            "S",
            "O",
            "A",
            "P",
            12.3m,
            38.2m,
            90,
            24,
            5,
            1,
            "Normal",
            "Dx",
            "Tx",
            "Resumen para casa"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await consultations.Received(1).AddAsync(Arg.Any<ClinicalConsultation>(), Arg.Any<CancellationToken>());
        await audit.Received(1).AddAsync(
            Arg.Is<AuditLogEntry>(entry => entry.Action == AuditAction.ClinicalConsultationCreated),
            Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NoGrantOrRecentScan_ReturnsFailure()
    {
        var clinicUserId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var clinic = Clinic.Create(clinicUserId, "Clinica Test", "VET-123", "San Jose", 9.93m, -84.08m, "clinic@test.cr");
        clinic.Activate();
        var veterinarian = ClinicVeterinarian.Create(clinic.Id, "Dra. Ana", "VET-999");
        var pet = Pet.Create(ownerId, "Nala", PetSpecies.Dog, "Criolla", DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-3)));
        var appointment = VeterinarianAppointment.Schedule(clinic.Id, veterinarian.Id, pet.Id, DateTimeOffset.UtcNow.AddHours(1), TimeSpan.FromMinutes(30), clinicUserId);
        appointment.Confirm();
        appointment.CheckIn();
        appointment.StartConsultation();

        var clinics = Substitute.For<IClinicRepository>();
        clinics.GetByIdAsync(clinic.Id, Arg.Any<CancellationToken>()).Returns(clinic);
        var appointments = Substitute.For<IVeterinarianAppointmentRepository>();
        appointments.GetByIdAsync(appointment.Id, Arg.Any<CancellationToken>()).Returns(appointment);
        var veterinarians = Substitute.For<IClinicVeterinarianRepository>();
        veterinarians.GetByIdAsync(veterinarian.Id, Arg.Any<CancellationToken>()).Returns(veterinarian);
        var pets = Substitute.For<IPetRepository>();
        pets.GetByIdAsync(pet.Id, Arg.Any<CancellationToken>()).Returns(pet);
        var consultations = Substitute.For<IClinicalConsultationRepository>();
        var grants = Substitute.For<IClinicMedicalAccessGrantRepository>();
        var scans = Substitute.For<IClinicScanRepository>();

        var handler = new CreateClinicalConsultationCommandHandler(
            clinics,
            appointments,
            veterinarians,
            pets,
            consultations,
            grants,
            scans,
            Substitute.For<IAuditLogRepository>(),
            Substitute.For<IUnitOfWork>(),
            Substitute.For<IClinicStaffAccessRepository>());

        var result = await handler.Handle(new CreateClinicalConsultationCommand(
            clinic.Id, clinicUserId, appointment.Id, "Control", "S", "O", "A", "P",
            null, null, null, null, null, null, null, "Dx", "Tx", "Resumen"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain("La clínica no tiene acceso de escritura al expediente de esta mascota.");
    }
}
