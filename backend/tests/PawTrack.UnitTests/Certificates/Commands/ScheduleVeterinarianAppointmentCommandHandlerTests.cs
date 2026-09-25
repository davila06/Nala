using FluentAssertions;
using NSubstitute;
using PawTrack.Application.Certificates.Commands.ScheduleVeterinarianAppointment;
using PawTrack.Application.Certificates.Interfaces;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Audit;
using PawTrack.Domain.Certificates;
using PawTrack.Domain.Clinics;
using PawTrack.Domain.Medical;
using PawTrack.Domain.Pets;

namespace PawTrack.UnitTests.Certificates.Commands;

public sealed class ScheduleVeterinarianAppointmentCommandHandlerTests
{
    [Fact]
    public async Task Handle_ValidAppointment_CreatesOwnerReminder()
    {
        var clinicUserId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var clinic = Clinic.Create(clinicUserId, "Clinica Test", "VET-123", "San Jose", 9.93m, -84.08m, "clinic@test.cr");
        clinic.Activate();
        var veterinarian = ClinicVeterinarian.Create(clinic.Id, "Dra. Ana", "VET-999");
        var pet = Pet.Create(ownerId, "Nala", PetSpecies.Dog, "Criolla", DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-3)));

        var clinics = Substitute.For<IClinicRepository>();
        clinics.GetByIdAsync(clinic.Id, Arg.Any<CancellationToken>()).Returns(clinic);
        var veterinarians = Substitute.For<IClinicVeterinarianRepository>();
        veterinarians.GetByIdAsync(veterinarian.Id, Arg.Any<CancellationToken>()).Returns(veterinarian);
        var appointments = Substitute.For<IVeterinarianAppointmentRepository>();
        var blocks = Substitute.For<IVeterinarianScheduleBlockRepository>();
        var auditLog = Substitute.For<IAuditLogRepository>();
        var pets = Substitute.For<IPetRepository>();
        pets.GetByIdAsync(pet.Id, Arg.Any<CancellationToken>()).Returns(pet);
        var medical = Substitute.For<IMedicalRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();

        var handler = new ScheduleVeterinarianAppointmentCommandHandler(
            clinics,
            veterinarians,
            appointments,
            blocks,
            pets,
            medical,
            auditLog,
            unitOfWork);

        var startsAt = DateTimeOffset.UtcNow.AddDays(1);
        var result = await handler.Handle(
            new ScheduleVeterinarianAppointmentCommand(clinic.Id, clinicUserId, veterinarian.Id, pet.Id, startsAt, 30),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await medical.Received(1).AddReminderAsync(
            Arg.Is<VetReminder>(reminder =>
                reminder.PetId == pet.Id &&
                reminder.OwnerId == ownerId &&
                reminder.Type == MedicalRecordType.Checkup &&
                reminder.Title.Contains("Cita veterinaria")),
            Arg.Any<CancellationToken>());
    }
}
