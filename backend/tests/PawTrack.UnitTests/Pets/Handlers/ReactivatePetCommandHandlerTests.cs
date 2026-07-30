using FluentAssertions;
using NSubstitute;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Pets.Commands.ReactivatePet;
using PawTrack.Domain.Pets;

namespace PawTrack.UnitTests.Pets.Handlers;

public sealed class ReactivatePetCommandHandlerTests
{
    private readonly IPetRepository _petRepo = Substitute.For<IPetRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly ReactivatePetCommandHandler _sut;

    public ReactivatePetCommandHandlerTests()
    {
        _sut = new ReactivatePetCommandHandler(_petRepo, _uow);
    }

    [Fact]
    public async Task Handle_ReunitedPet_ReactivatesAndPersists()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var pet = Pet.Create(ownerId, "Luna", PetSpecies.Cat, null, null);
        pet.MarkAsReunited();

        _petRepo.GetByIdAsync(pet.Id, Arg.Any<CancellationToken>()).Returns(pet);
        _uow.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

        // Act
        var result = await _sut.Handle(new ReactivatePetCommand(pet.Id, ownerId), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        pet.Status.Should().Be(PetStatus.Active);
        _petRepo.Received(1).Update(pet);
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_PetNotFound_ReturnsFailure()
    {
        // Arrange
        _petRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Pet?)null);

        // Act
        var result = await _sut.Handle(
            new ReactivatePetCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Contains("not found"));
        _petRepo.DidNotReceive().Update(Arg.Any<Pet>());
    }

    [Fact]
    public async Task Handle_WrongOwner_ReturnsForbidden()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var attacker = Guid.NewGuid();
        var pet = Pet.Create(ownerId, "Max", PetSpecies.Dog, null, null);
        pet.MarkAsReunited();

        _petRepo.GetByIdAsync(pet.Id, Arg.Any<CancellationToken>()).Returns(pet);

        // Act
        var result = await _sut.Handle(new ReactivatePetCommand(pet.Id, attacker), CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Contains("Access denied"));
        _petRepo.DidNotReceive().Update(Arg.Any<Pet>());
        await _uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ActivePet_ReturnsDomainFailure()
    {
        // Arrange — pet is Active, not Reunited
        var ownerId = Guid.NewGuid();
        var pet = Pet.Create(ownerId, "Kira", PetSpecies.Dog, null, null);

        _petRepo.GetByIdAsync(pet.Id, Arg.Any<CancellationToken>()).Returns(pet);

        // Act
        var result = await _sut.Handle(new ReactivatePetCommand(pet.Id, ownerId), CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Contains("reunited"));
        await _uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
