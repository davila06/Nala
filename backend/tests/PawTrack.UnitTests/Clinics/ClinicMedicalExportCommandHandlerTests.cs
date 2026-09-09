using FluentAssertions;
using NSubstitute;
using PawTrack.Application.Clinics.Commands.ExportClinicMedical;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Medical;
using PawTrack.Domain.Clinics;
using PawTrack.Domain.Medical;
using PawTrack.Domain.Pets;

namespace PawTrack.UnitTests.Clinics;

public sealed class ClinicMedicalExportCommandHandlerTests
{
    [Fact]
    public async Task Handle_GrantWithoutExportPermission_IsRejected()
    {
        var clinicUserId = Guid.NewGuid();
        var clinic = Clinic.Create(clinicUserId, "Vet", "SENASA-1", "San Jose", 9.9m, -84m, "vet@example.com");
        clinic.Activate();
        var pet = Pet.Create(Guid.NewGuid(), "Max", PetSpecies.Dog, null, null);
        var (grant, code) = ClinicMedicalAccessGrant.Generate(pet.Id, clinic.Id, pet.OwnerId, "Owner", permissions: [ClinicMedicalAccessPermission.Read]);
        grant.TryAccept(code);

        var clinics = Substitute.For<IClinicRepository>();
        var pets = Substitute.For<IPetRepository>();
        var grants = Substitute.For<IClinicMedicalAccessGrantRepository>();
        var records = Substitute.For<IMedicalRepository>();
        clinics.GetByIdAsync(clinic.Id, Arg.Any<CancellationToken>()).Returns(clinic);
        pets.GetByIdAsync(pet.Id, Arg.Any<CancellationToken>()).Returns(pet);
        grants.GetActiveGrantAsync(clinic.Id, pet.Id, Arg.Any<CancellationToken>()).Returns(grant);

        var handler = new ExportClinicMedicalCommandHandler(
            clinics, pets, grants, records, Substitute.For<IMedicalPdfExporter>(),
            Substitute.For<IClinicMedicalExportRepository>(), Substitute.For<IBlobStorageService>(), Substitute.For<IUnitOfWork>());

        var result = await handler.Handle(new ExportClinicMedicalCommand(clinic.Id, clinicUserId, pet.Id), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Contains("export", StringComparison.OrdinalIgnoreCase));
    }
}