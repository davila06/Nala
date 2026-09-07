using FluentAssertions;
using NSubstitute;
using PawTrack.Application.Certificates.Interfaces;
using PawTrack.Application.Certificates.Queries.GetCertificatesForClinic;
using PawTrack.Application.Certificates.Queries.GetCertificatesForPet;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Clinics;
using PawTrack.Domain.Pets;

namespace PawTrack.UnitTests.Certificates.Queries;

public sealed class GetCertificatesAuthorizationTests
{
    [Fact]
    public async Task GetForPet_UnrelatedUser_ReturnsAccessDenied()
    {
        var certificateRepository = Substitute.For<ICertificateRepository>();
        var petRepository = Substitute.For<IPetRepository>();
        var familyRepository = Substitute.For<IFamilyRepository>();
        var pet = Pet.Create(Guid.NewGuid(), "Nala", PetSpecies.Dog, null, null);
        petRepository.GetByIdAsync(pet.Id, Arg.Any<CancellationToken>()).Returns(pet);
        familyRepository.GetActiveMemberIdsAsync(pet.OwnerId, Arg.Any<CancellationToken>()).Returns([]);

        var handler = new GetCertificatesForPetQueryHandler(certificateRepository, petRepository, familyRepository);

        var result = await handler.Handle(new GetCertificatesForPetQuery(pet.Id, Guid.NewGuid(), IsAdmin: false), default);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain("Acceso denegado.");
        await certificateRepository.DidNotReceive().GetForPetAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetForClinic_NonClinicOwner_ReturnsAccessDenied()
    {
        var certificateRepository = Substitute.For<ICertificateRepository>();
        var clinicRepository = Substitute.For<IClinicRepository>();
        var clinic = Clinic.Create(Guid.NewGuid(), "VetSalud", "SENASA-12345", "Heredia", 10m, -84.1m, "vet@example.com");
        clinicRepository.GetByIdAsync(clinic.Id, Arg.Any<CancellationToken>()).Returns(clinic);

        var handler = new GetCertificatesForClinicQueryHandler(certificateRepository, clinicRepository);

        var result = await handler.Handle(new GetCertificatesForClinicQuery(clinic.Id, Guid.NewGuid(), IsAdmin: false), default);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain("Acceso denegado.");
        await certificateRepository.DidNotReceive().GetForClinicAsync(Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }
}
