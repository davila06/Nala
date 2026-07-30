using FluentAssertions;
using PawTrack.Domain.Pets;
using PawTrack.Domain.Pets.Events;

namespace PawTrack.UnitTests.Pets.Domain;

public sealed class PetReactivateTests
{
    [Fact]
    public void Reactivate_FromReunited_Succeeds()
    {
        var pet = Pet.Create(Guid.NewGuid(), "Luna", PetSpecies.Cat, null, null);
        pet.MarkAsReunited();

        var result = pet.Reactivate();

        result.IsSuccess.Should().BeTrue();
        pet.Status.Should().Be(PetStatus.Active);
        pet.DomainEvents.Should().ContainSingle(e => e is PetReactivatedDomainEvent);
    }

    [Fact]
    public void Reactivate_FromActive_ReturnsFailure()
    {
        var pet = Pet.Create(Guid.NewGuid(), "Max", PetSpecies.Dog, null, null);

        var result = pet.Reactivate();

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Contains("reunited"));
        pet.Status.Should().Be(PetStatus.Active); // unchanged
    }

    [Fact]
    public void Reactivate_FromLost_ReturnsFailure()
    {
        var pet = Pet.Create(Guid.NewGuid(), "Rex", PetSpecies.Dog, null, null);
        pet.MarkAsLost();

        var result = pet.Reactivate();

        result.IsFailure.Should().BeTrue();
        pet.Status.Should().Be(PetStatus.Lost); // unchanged
    }
}
