using FluentAssertions;
using NSubstitute;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Pets.Queries.GetMyPets;
using PawTrack.Application.Pets.Queries.GetPetDetail;
using PawTrack.Domain.Pets;

namespace PawTrack.UnitTests.Pets.Queries;

public sealed class PetQueriesAuthorizationTests
{
    private readonly IPetRepository _petRepo = Substitute.For<IPetRepository>();

    // ── GetMyPets ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetMyPets_ReturnsOnlyOwnersPets()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var pets = new List<Pet>
        {
            Pet.Create(ownerId, "Rex", PetSpecies.Dog, null, null),
            Pet.Create(ownerId, "Mia", PetSpecies.Cat, null, null),
        };

        _petRepo.GetByOwnerIdAsync(ownerId, Arg.Any<CancellationToken>())
            .Returns(pets.AsReadOnly());

        var handler = new GetMyPetsQueryHandler(_petRepo);

        // Act
        var result = await handler.Handle(new GetMyPetsQuery(ownerId), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value!.Select(p => p.Name).Should().BeEquivalentTo(["Rex", "Mia"]);
    }

    // ── GetPetDetail ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetPetDetail_Owner_ReturnsPet()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var pet = Pet.Create(ownerId, "Rocky", PetSpecies.Dog, "Bulldog", null);

        _petRepo.GetByIdAsync(pet.Id, Arg.Any<CancellationToken>()).Returns(pet);

        var handler = new GetPetDetailQueryHandler(_petRepo);

        // Act
        var result = await handler.Handle(new GetPetDetailQuery(pet.Id, ownerId), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Rocky");
        result.Value.Breed.Should().Be("Bulldog");
    }

    [Fact]
    public async Task GetPetDetail_DifferentUser_ReturnsAccessDenied()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var pet = Pet.Create(ownerId, "Kira", PetSpecies.Cat, null, null);

        _petRepo.GetByIdAsync(pet.Id, Arg.Any<CancellationToken>()).Returns(pet);

        var handler = new GetPetDetailQueryHandler(_petRepo);

        // Act
        var result = await handler.Handle(new GetPetDetailQuery(pet.Id, otherUserId), CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle().Which.Should().Be("Access denied.");
    }

    [Fact]
    public async Task GetPetDetail_NonExistentPet_ReturnsFailure()
    {
        // Arrange
        _petRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Pet?)null);

        var handler = new GetPetDetailQueryHandler(_petRepo);

        // Act
        var result = await handler.Handle(
            new GetPetDetailQuery(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle().Which.Should().Be("Pet not found.");
    }
}
