using FluentAssertions;
using PawTrack.Domain.Pets;

namespace PawTrack.UnitTests.Pets.Domain;

public sealed class PetSanitaryIdentityTests
{
    [Fact]
    public void Create_DefaultsSanitaryIdentityToUnknownAndNotProvided()
    {
        var pet = Pet.Create(Guid.NewGuid(), "Nala", PetSpecies.Dog, null, null);

        pet.Sex.Should().Be(PetSex.Unknown);
        pet.SterilizedStatus.Should().Be(SterilizedStatus.Unknown);
        pet.MicrochipVerificationStatus.Should().Be(MicrochipVerificationStatus.NotProvided);
    }

    [Fact]
    public void UpdateSanitaryIdentity_WithNonSterilizedPet_ClearsSterilizedDate()
    {
        var pet = Pet.Create(Guid.NewGuid(), "Nala", PetSpecies.Dog, null, null);

        pet.UpdateSanitaryIdentity(
            PetSex.Female,
            "Dorado",
            "Pecho blanco",
            SterilizedStatus.No,
            new DateOnly(2026, 1, 1),
            "San José");

        pet.Sex.Should().Be(PetSex.Female);
        pet.Color.Should().Be("Dorado");
        pet.DistinctiveMarks.Should().Be("Pecho blanco");
        pet.SterilizedStatus.Should().Be(SterilizedStatus.No);
        pet.SterilizedAt.Should().BeNull();
        pet.ResidenceCanton.Should().Be("San José");
    }

    [Fact]
    public void SetMicrochipDeclared_ValidChip_SetsDeclaredStatus()
    {
        var pet = Pet.Create(Guid.NewGuid(), "Nala", PetSpecies.Dog, null, null);

        var result = pet.SetMicrochipDeclared(" 123456789012345 ");

        result.IsSuccess.Should().BeTrue();
        pet.MicrochipId.Should().Be("123456789012345");
        pet.MicrochipVerificationStatus.Should().Be(MicrochipVerificationStatus.Declared);
    }

    [Fact]
    public void VerifyMicrochip_MatchingChip_MarksAsVerified()
    {
        var pet = Pet.Create(Guid.NewGuid(), "Nala", PetSpecies.Dog, null, null);
        pet.SetMicrochipDeclared("123456789012345");
        var clinicId = Guid.NewGuid();

        var result = pet.VerifyMicrochip(clinicId, "123456789012345", "Leído en consulta");

        result.IsSuccess.Should().BeTrue();
        pet.MicrochipVerificationStatus.Should().Be(MicrochipVerificationStatus.Verified);
        pet.MicrochipVerifiedByClinicId.Should().Be(clinicId);
        pet.MicrochipVerifiedAt.Should().NotBeNull();
    }

    [Fact]
    public void VerifyMicrochip_DifferentChip_FlagsConflict()
    {
        var pet = Pet.Create(Guid.NewGuid(), "Nala", PetSpecies.Dog, null, null);
        pet.SetMicrochipDeclared("123456789012345");

        var result = pet.VerifyMicrochip(Guid.NewGuid(), "999999999999999", "Chip leído no coincide");

        result.IsFailure.Should().BeTrue();
        pet.MicrochipVerificationStatus.Should().Be(MicrochipVerificationStatus.Conflict);
        pet.MicrochipVerificationNotes.Should().Contain("999999999999999");
    }
}
