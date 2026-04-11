using FluentAssertions;
using NSubstitute;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Pets.Commands.DeletePet;
using PawTrack.Domain.Pets;

namespace PawTrack.UnitTests.Pets.Handlers;

public sealed class DeletePetCommandHandlerTests
{
    private readonly IPetRepository _petRepo = Substitute.For<IPetRepository>();
    private readonly IBlobStorageService _blobStorage = Substitute.For<IBlobStorageService>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    private readonly DeletePetCommandHandler _sut;

    public DeletePetCommandHandlerTests()
    {
        _sut = new DeletePetCommandHandler(_petRepo, _blobStorage, _uow);
    }

    [Fact]
    public async Task Handle_ExistingPetWithPhoto_DeletesBlobAndRecord()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var pet = Pet.Create(ownerId, "Max", PetSpecies.Dog, null, null);
        pet.SetPhoto("https://example.blob.core.windows.net/pet-photos/max.jpg");

        _petRepo.GetByIdAsync(pet.Id, Arg.Any<CancellationToken>()).Returns(pet);
        _uow.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

        // Act
        var result = await _sut.Handle(new DeletePetCommand(pet.Id, ownerId), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();

        await _blobStorage.Received(1).DeleteAsync(
            "https://example.blob.core.windows.net/pet-photos/max.jpg",
            Arg.Any<CancellationToken>());

        _petRepo.Received(1).Delete(pet);
    }

    [Fact]
    public async Task Handle_ExistingPetWithoutPhoto_DeletesRecordNoBlobCall()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var pet = Pet.Create(ownerId, "Mia", PetSpecies.Cat, null, null);

        _petRepo.GetByIdAsync(pet.Id, Arg.Any<CancellationToken>()).Returns(pet);
        _uow.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

        // Act
        var result = await _sut.Handle(new DeletePetCommand(pet.Id, ownerId), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        await _blobStorage.DidNotReceive().DeleteAsync(
            Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_PetNotFound_ReturnsFailure()
    {
        // Arrange
        var cmd = new DeletePetCommand(Guid.NewGuid(), Guid.NewGuid());
        _petRepo.GetByIdAsync(cmd.PetId, Arg.Any<CancellationToken>()).Returns((Pet?)null);

        // Act
        var result = await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle().Which.Should().Be("Pet not found.");
    }

    [Fact]
    public async Task Handle_DifferentOwner_ReturnsAccessDenied()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var pet = Pet.Create(ownerId, "Bruno", PetSpecies.Dog, null, null);

        _petRepo.GetByIdAsync(pet.Id, Arg.Any<CancellationToken>()).Returns(pet);

        // Act
        var result = await _sut.Handle(new DeletePetCommand(pet.Id, otherUserId), CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle().Which.Should().Be("Access denied.");

        _petRepo.DidNotReceive().Delete(Arg.Any<Pet>());
    }
}
