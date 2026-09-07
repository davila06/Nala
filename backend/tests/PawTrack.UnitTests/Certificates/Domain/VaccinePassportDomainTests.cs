using FluentAssertions;
using PawTrack.Domain.Certificates;
using PawTrack.Domain.Pets;

namespace PawTrack.UnitTests.Certificates.Domain;

public sealed class VaccinePassportDomainTests
{
    [Fact]
    public void Issue_WithValidRabiesVaccine_CreatesStructuredPassport()
    {
        var certificateId = Guid.NewGuid();
        var petId = Guid.NewGuid();
        var clinicId = Guid.NewGuid();
        var veterinarianId = Guid.NewGuid();

        var passport = VaccinePassport.Issue(
            certificateId,
            petId,
            clinicId,
            veterinarianId,
            new VaccinePassportPetSnapshot("Nala", PetSpecies.Dog.ToString(), null, null, "Dorado", "123456789012345", "Denis"),
            new VaccinePassportIssuerSnapshot("VetSalud", "SENASA-12345", "Dra. Rivera", "VET-12345"),
            [new VaccinePassportVaccine("Rabia", "Brand", "LOT-1", new DateOnly(2026, 1, 1), new DateOnly(2027, 1, 1))],
            new VaccinePassportParasiteControl("Antiparasitario", new DateOnly(2026, 1, 2), new DateOnly(2026, 4, 2)),
            new DateOnly(2027, 1, 1),
            "ABCD1234");

        passport.CertificateId.Should().Be(certificateId);
        passport.PetId.Should().Be(petId);
        passport.IssuingClinicId.Should().Be(clinicId);
        passport.IssuingVeterinarianId.Should().Be(veterinarianId);
        passport.Vaccines.Should().ContainSingle(v => v.Name == "Rabia");
        passport.ParasiteControl.Should().NotBeNull();
        passport.VerificationCode.Should().Be("ABCD1234");
    }

    [Fact]
    public void Issue_WithoutVaccines_ReturnsFailure()
    {
        var result = VaccinePassport.TryIssue(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            new VaccinePassportPetSnapshot("Nala", PetSpecies.Dog.ToString(), null, null, "Dorado", null, "Denis"),
            new VaccinePassportIssuerSnapshot("VetSalud", "SENASA-12345", "Dra. Rivera", "VET-12345"),
            [],
            null,
            new DateOnly(2027, 1, 1),
            "ABCD1234");

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain("Al menos una vacuna es requerida para emitir el pasaporte.");
    }
}
