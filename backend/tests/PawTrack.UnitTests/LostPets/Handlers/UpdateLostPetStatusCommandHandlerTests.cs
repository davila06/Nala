using FluentAssertions;
using NSubstitute;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.LostPets.Commands.UpdateLostPetStatus;
using PawTrack.Domain.Auth;
using PawTrack.Domain.LostPets;
using PawTrack.Domain.Pets;
using PawTrack.Domain.Sightings;

namespace PawTrack.UnitTests.LostPets.Handlers;

public sealed class UpdateLostPetStatusCommandHandlerTests
{
    private readonly ILostPetRepository _lostPetRepo = Substitute.For<ILostPetRepository>();
    private readonly IPetRepository _petRepo = Substitute.For<IPetRepository>();
    private readonly IUserRepository _userRepo = Substitute.For<IUserRepository>();
    private readonly ISightingRepository _sightingRepo = Substitute.For<ISightingRepository>();
    private readonly IReverseGeocodingService _reverseGeocoding = Substitute.For<IReverseGeocodingService>();
    private readonly INotificationDispatcher _dispatcher = Substitute.For<INotificationDispatcher>();
    private readonly IContributorScoreRepository _contributorScoreRepo = Substitute.For<IContributorScoreRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    private readonly UpdateLostPetStatusCommandHandler _sut;

    public UpdateLostPetStatusCommandHandlerTests()
    {
        _sut = new UpdateLostPetStatusCommandHandler(
            _lostPetRepo,
            _petRepo,
            _userRepo,
            _sightingRepo,
            _reverseGeocoding,
            _dispatcher,
            _contributorScoreRepo,
            _uow);
    }

    private static LostPetEvent CreateLostReport(Guid petId, Guid ownerId)
        => LostPetEvent.Create(petId, ownerId, "Missing", 9.9, -84.0, DateTimeOffset.UtcNow.AddHours(-1));

    [Fact]
    public async Task Handle_MarkAsReunited_UpdatesBothReportAndPet()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var pet = Pet.Create(ownerId, "Max", PetSpecies.Dog, null, null);
        pet.MarkAsLost();
        var report = CreateLostReport(pet.Id, ownerId);
        var (user, _) = User.Create("owner@test.com", "hash", "Max Owner");

        _lostPetRepo.GetByIdAsync(report.Id, Arg.Any<CancellationToken>()).Returns(report);
        _petRepo.GetByIdAsync(pet.Id, Arg.Any<CancellationToken>()).Returns(pet);
        _userRepo.GetByIdAsync(ownerId, Arg.Any<CancellationToken>()).Returns(user);
        _uow.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

        var command = new UpdateLostPetStatusCommand(report.Id, ownerId, LostPetStatus.Reunited);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        report.Status.Should().Be(LostPetStatus.Reunited);
        report.ResolvedAt.Should().NotBeNull();
        pet.Status.Should().Be(PetStatus.Reunited);
        _petRepo.Received(1).Update(pet);
        await _dispatcher.Received(1).DispatchPetReunitedAsync(
            Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(), "Max",
            Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_CancelReport_ReactivatePet()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var pet = Pet.Create(ownerId, "Mia", PetSpecies.Cat, null, null);
        pet.MarkAsLost();
        var report = CreateLostReport(pet.Id, ownerId);

        _lostPetRepo.GetByIdAsync(report.Id, Arg.Any<CancellationToken>()).Returns(report);
        _petRepo.GetByIdAsync(pet.Id, Arg.Any<CancellationToken>()).Returns(pet);
        _uow.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

        var command = new UpdateLostPetStatusCommand(report.Id, ownerId, LostPetStatus.Cancelled);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        report.Status.Should().Be(LostPetStatus.Cancelled);
        pet.Status.Should().Be(PetStatus.Active);
        await _dispatcher.DidNotReceive().DispatchPetReunitedAsync(
            Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AlreadyResolved_ReturnsFailure()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var pet = Pet.Create(ownerId, "Buddy", PetSpecies.Dog, null, null);
        var report = CreateLostReport(pet.Id, ownerId);
        report.Resolve(LostPetStatus.Reunited); // already resolved

        _lostPetRepo.GetByIdAsync(report.Id, Arg.Any<CancellationToken>()).Returns(report);

        var command = new UpdateLostPetStatusCommand(report.Id, ownerId, LostPetStatus.Cancelled);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain("Only active reports can be resolved.");
    }

    [Fact]
    public async Task Handle_ReportNotFound_ReturnsFailure()
    {
        // Arrange
        _lostPetRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((LostPetEvent?)null);

        var command = new UpdateLostPetStatusCommand(Guid.NewGuid(), Guid.NewGuid(), LostPetStatus.Reunited);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain("Lost pet report not found.");
    }

    [Fact]
    public async Task Handle_DifferentOwner_ReturnsAccessDenied()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var attackerId = Guid.NewGuid();
        var pet = Pet.Create(ownerId, "Max", PetSpecies.Dog, null, null);
        var report = CreateLostReport(pet.Id, ownerId);

        _lostPetRepo.GetByIdAsync(report.Id, Arg.Any<CancellationToken>()).Returns(report);

        var command = new UpdateLostPetStatusCommand(report.Id, attackerId, LostPetStatus.Reunited);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain("Access denied.");
    }

    [Fact]
    public async Task Handle_Reunited_WithConfirmedSighting_SetsRecoveryMetadata()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var pet = Pet.Create(ownerId, "Nala", PetSpecies.Cat, null, null);
        pet.MarkAsLost();

        var report = CreateLostReport(pet.Id, ownerId);
        var sighting = Sighting.Create(
            pet.Id,
            report.Id,
            9.9401,
            -84.0502,
            "seen near bakery",
            DateTimeOffset.UtcNow.AddMinutes(-20));

        var (user, _) = User.Create("owner@nala.test", "hash", "Nala Owner");

        _lostPetRepo.GetByIdAsync(report.Id, Arg.Any<CancellationToken>()).Returns(report);
        _petRepo.GetByIdAsync(pet.Id, Arg.Any<CancellationToken>()).Returns(pet);
        _userRepo.GetByIdAsync(ownerId, Arg.Any<CancellationToken>()).Returns(user);
        _sightingRepo.GetByIdAsync(sighting.Id, Arg.Any<CancellationToken>()).Returns(sighting);
        _reverseGeocoding.ResolveCantonAsync(
            sighting.Lat,
            sighting.Lng,
            Arg.Any<CancellationToken>()).Returns("Montes de Oca");
        _uow.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

        var command = new UpdateLostPetStatusCommand(
            report.Id,
            ownerId,
            LostPetStatus.Reunited,
            sighting.Id);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        report.ReunionLat.Should().Be(sighting.Lat);
        report.ReunionLng.Should().Be(sighting.Lng);
        report.CantonName.Should().Be("Montes de Oca");
        report.RecoveryDistanceMeters.Should().NotBeNull();
        report.RecoveryTime.Should().NotBeNull();
    }
}
