using FluentAssertions;
using NSubstitute;
using PawTrack.Application.Certificates.Queries.GetClinicAgenda;
using PawTrack.Application.Certificates.Interfaces;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Certificates;
using PawTrack.Domain.Clinics;
using PawTrack.Domain.Pets;

namespace PawTrack.UnitTests.Certificates.Queries;

public sealed class GetClinicAgendaQueryHandlerTests
{
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

        var handler = new GetClinicAgendaQueryHandler(clinics, appointments, pets, veterinarians);

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
