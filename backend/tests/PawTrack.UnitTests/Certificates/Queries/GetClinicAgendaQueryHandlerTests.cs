using FluentAssertions;
using NSubstitute;
using PawTrack.Application.Certificates.Queries.GetClinicAgenda;
using PawTrack.Application.Certificates.Interfaces;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Certificates;
using PawTrack.Domain.Clinics;
using PawTrack.Domain.Pets;
using PawTrack.Application.Clinics.Interfaces;

namespace PawTrack.UnitTests.Certificates.Queries;

public sealed class GetClinicAgendaQueryHandlerTests
{
    [Fact]
    public async Task Receptionist_ViewsOnlyItsClinicsAgenda()
    {
        var clinic = Clinic.Create(Guid.NewGuid(), "Clinica Test", "VET-123", "San Jose", 9.93m, -84.08m, "clinic@test.cr");
        var foreign = Clinic.Create(Guid.NewGuid(), "Otra Clinica", "VET-999", "San Jose", 9.93m, -84.08m, "foreign@test.cr");
        var receptionistId = Guid.NewGuid();
        var clinics = Substitute.For<IClinicRepository>();
        clinics.GetByIdAsync(clinic.Id, Arg.Any<CancellationToken>()).Returns(clinic);
        clinics.GetByIdAsync(foreign.Id, Arg.Any<CancellationToken>()).Returns(foreign);
        var staff = Substitute.For<IClinicStaffAccessRepository>();
        staff.HasPermissionAsync(clinic.Id, receptionistId, ClinicStaffPermission.ViewAgenda, Arg.Any<CancellationToken>()).Returns(true);
        var handler = new GetClinicAgendaQueryHandler(clinics, Substitute.For<IVeterinarianAppointmentRepository>(),
            Substitute.For<IPetRepository>(), Substitute.For<IClinicVeterinarianRepository>(), staff);
        var from = DateTimeOffset.UtcNow;

        var allowed = await handler.Handle(new GetClinicAgendaQuery(clinic.Id, receptionistId, from, from.AddDays(1)), CancellationToken.None);
        var denied = await handler.Handle(new GetClinicAgendaQuery(foreign.Id, receptionistId, from, from.AddDays(1)), CancellationToken.None);

        allowed.IsSuccess.Should().BeTrue();
        denied.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ClinicOwnedAgenda_ReturnsAppointmentNames()
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

        var veterinarian = ClinicVeterinarian.Create(clinic.Id, "Dra. Ana Mora", "VET-999");
        var pet = Pet.Create(
            clinicUserId,
            "Nala",
            PetSpecies.Dog,
            "Criolla",
            DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-3)));
        var appointment = VeterinarianAppointment.Schedule(
            clinic.Id,
            veterinarian.Id,
            pet.Id,
            DateTimeOffset.UtcNow.AddHours(2),
            TimeSpan.FromMinutes(30),
            clinicUserId);

        var clinics = Substitute.For<IClinicRepository>();
        clinics.GetByIdAsync(clinic.Id, Arg.Any<CancellationToken>()).Returns(clinic);

        var appointments = Substitute.For<IVeterinarianAppointmentRepository>();
        appointments.GetForClinicAsync(clinic.Id, Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns([appointment]);

        var pets = Substitute.For<IPetRepository>();
        pets.GetByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>()).Returns([pet]);

        var veterinarians = Substitute.For<IClinicVeterinarianRepository>();
        veterinarians.GetByClinicAsync(clinic.Id, Arg.Any<CancellationToken>()).Returns([veterinarian]);

        var handler = new GetClinicAgendaQueryHandler(clinics, appointments, pets, veterinarians,
            Substitute.For<IClinicStaffAccessRepository>());

        var result = await handler.Handle(
            new GetClinicAgendaQuery(
                clinic.Id,
                clinicUserId,
                DateTimeOffset.UtcNow.Date,
                DateTimeOffset.UtcNow.Date.AddDays(1)),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle();
        result.Value[0].PetName.Should().Be("Nala");
        result.Value[0].VeterinarianName.Should().Be("Dra. Ana Mora");
    }
}
