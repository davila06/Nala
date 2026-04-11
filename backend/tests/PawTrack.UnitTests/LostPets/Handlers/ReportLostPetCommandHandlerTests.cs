using FluentAssertions;
using NSubstitute;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.LostPets.Commands.ReportLostPet;
using PawTrack.Application.LostPets.Commands.UpdateLostPetStatus;
using PawTrack.Application.LostPets.SearchRadius;
using PawTrack.Domain.Auth;
using PawTrack.Domain.LostPets;
using PawTrack.Domain.Pets;

namespace PawTrack.UnitTests.LostPets.Handlers;

public sealed class ReportLostPetCommandHandlerTests
{
    private readonly IPetRepository _petRepo = Substitute.For<IPetRepository>();
    private readonly ILostPetRepository _lostPetRepo = Substitute.For<ILostPetRepository>();
    private readonly IUserRepository _userRepo = Substitute.For<IUserRepository>();
    private readonly INotificationDispatcher _dispatcher = Substitute.For<INotificationDispatcher>();
    private readonly IBlobStorageService _blobStorage = Substitute.For<IBlobStorageService>();
    private readonly IImageProcessor _imageProcessor = Substitute.For<IImageProcessor>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly ILostPetSearchRadiusCalculator _searchRadiusCalculator = new LostPetSearchRadiusCalculator();

    private readonly ReportLostPetCommandHandler _sut;

    public ReportLostPetCommandHandlerTests()
    {
        _sut = new ReportLostPetCommandHandler(
            _lostPetRepo, _petRepo, _userRepo, _dispatcher, _blobStorage, _imageProcessor, _uow, _searchRadiusCalculator);
    }

    [Fact]
    public async Task Handle_ValidRequest_CreatesReportAndMarksPetAsLost()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var pet = Pet.Create(ownerId, "Max", PetSpecies.Dog, null, null);
        var (user, _) = User.Create("owner@test.com", "Max Owner", Guid.NewGuid().ToString("N"));

        _petRepo.GetByIdAsync(pet.Id, Arg.Any<CancellationToken>()).Returns(pet);
        _lostPetRepo.GetActiveByPetIdAsync(pet.Id, Arg.Any<CancellationToken>()).Returns((LostPetEvent?)null);
        _userRepo.GetByIdAsync(ownerId, Arg.Any<CancellationToken>()).Returns(user);
        _uow.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

        var command = new ReportLostPetCommand(
            PetId: pet.Id,
            RequestingUserId: ownerId,
            Description: "Last seen near the park",
            PublicMessage: null,
            LastSeenLat: 9.9281,
            LastSeenLng: -84.0907,
            LastSeenAt: DateTimeOffset.UtcNow.AddHours(-2),
            PhotoBytes: null,
            PhotoContentType: null,
            PhotoFileName: null,
            ContactName: null,
            ContactPhone: null,
            RewardAmount: null,
            RewardNote: null);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNullOrEmpty();

        pet.Status.Should().Be(PetStatus.Lost);
        _petRepo.Received(1).Update(pet);
        await _lostPetRepo.Received(1).AddAsync(Arg.Any<LostPetEvent>(), Arg.Any<CancellationToken>());
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _dispatcher.Received(1).DispatchLostPetAlertAsync(
            Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(), "Max",
            Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_PetNotFound_ReturnsFailure()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        _petRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Pet?)null);

        var command = new ReportLostPetCommand(
            PetId: Guid.NewGuid(),
            RequestingUserId: ownerId,
            Description: null,
            PublicMessage: null,
            LastSeenLat: null,
            LastSeenLng: null,
            LastSeenAt: DateTimeOffset.UtcNow,
            PhotoBytes: null,
            PhotoContentType: null,
            PhotoFileName: null,
            ContactName: null,
            ContactPhone: null,
            RewardAmount: null,
            RewardNote: null);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain("Pet not found.");
    }

    [Fact]
    public async Task Handle_DifferentOwner_ReturnsAccessDenied()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var attackerId = Guid.NewGuid();
        var pet = Pet.Create(ownerId, "Max", PetSpecies.Dog, null, null);
        _petRepo.GetByIdAsync(pet.Id, Arg.Any<CancellationToken>()).Returns(pet);

        var command = new ReportLostPetCommand(
            PetId: pet.Id,
            RequestingUserId: attackerId,
            Description: null,
            PublicMessage: null,
            LastSeenLat: null,
            LastSeenLng: null,
            LastSeenAt: DateTimeOffset.UtcNow,
            PhotoBytes: null,
            PhotoContentType: null,
            PhotoFileName: null,
            ContactName: null,
            ContactPhone: null,
            RewardAmount: null,
            RewardNote: null);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain("Access denied.");
    }

    [Fact]
    public async Task Handle_AlreadyHasActiveReport_ReturnsFailure()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var pet = Pet.Create(ownerId, "Max", PetSpecies.Dog, null, null);
        var existingReport = LostPetEvent.Create(
            pet.Id, ownerId, null, null, null, DateTimeOffset.UtcNow);

        _petRepo.GetByIdAsync(pet.Id, Arg.Any<CancellationToken>()).Returns(pet);
        _lostPetRepo.GetActiveByPetIdAsync(pet.Id, Arg.Any<CancellationToken>()).Returns(existingReport);

        var command = new ReportLostPetCommand(
            PetId: pet.Id,
            RequestingUserId: ownerId,
            Description: null,
            PublicMessage: null,
            LastSeenLat: null,
            LastSeenLng: null,
            LastSeenAt: DateTimeOffset.UtcNow,
            PhotoBytes: null,
            PhotoContentType: null,
            PhotoFileName: null,
            ContactName: null,
            ContactPhone: null,
            RewardAmount: null,
            RewardNote: null);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain("This pet already has an active lost report.");
    }

    [Fact]
    public async Task Handle_WithCoordinates_UsesDynamicGeofenceRadius()
    {
        var ownerId = Guid.NewGuid();
        var pet = Pet.Create(ownerId, "Max", PetSpecies.Dog, "Labrador", null);
        var (user, _) = User.Create("owner@test.com", "Max Owner", Guid.NewGuid().ToString("N"));

        _petRepo.GetByIdAsync(pet.Id, Arg.Any<CancellationToken>()).Returns(pet);
        _lostPetRepo.GetActiveByPetIdAsync(pet.Id, Arg.Any<CancellationToken>()).Returns((LostPetEvent?)null);
        _userRepo.GetByIdAsync(ownerId, Arg.Any<CancellationToken>()).Returns(user);
        _uow.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

        var command = new ReportLostPetCommand(
            PetId: pet.Id,
            RequestingUserId: ownerId,
            Description: "Last seen near the park",
            PublicMessage: null,
            LastSeenLat: 9.9281,
            LastSeenLng: -84.0907,
            LastSeenAt: DateTimeOffset.UtcNow.AddHours(-30),
            PhotoBytes: null,
            PhotoContentType: null,
            PhotoFileName: null,
            ContactName: null,
            ContactPhone: null,
            RewardAmount: null,
            RewardNote: null);

        await _sut.Handle(command, CancellationToken.None);

        await _dispatcher.Received(1).DispatchGeofencedLostPetAlertsAsync(
            Arg.Any<Guid>(),
            "Max",
            nameof(PetSpecies.Dog),
            "Labrador",
            9.9281,
            -84.0907,
            1500,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithCoordinates_DispatchesVerifiedAllyAlerts()
    {
        var ownerId = Guid.NewGuid();
        var pet = Pet.Create(ownerId, "Luna", PetSpecies.Cat, null, null);
        var (user, _) = User.Create("owner@test.com", "owner-password", "Luna Owner");

        _petRepo.GetByIdAsync(pet.Id, Arg.Any<CancellationToken>()).Returns(pet);
        _lostPetRepo.GetActiveByPetIdAsync(pet.Id, Arg.Any<CancellationToken>()).Returns((LostPetEvent?)null);
        _userRepo.GetByIdAsync(ownerId, Arg.Any<CancellationToken>()).Returns(user);
        _uow.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

        var command = new ReportLostPetCommand(
            PetId: pet.Id,
            RequestingUserId: ownerId,
            Description: "Se escapó cerca del parque",
            PublicMessage: null,
            LastSeenLat: 9.9281,
            LastSeenLng: -84.0907,
            LastSeenAt: DateTimeOffset.UtcNow.AddHours(-1),
            PhotoBytes: null,
            PhotoContentType: null,
            PhotoFileName: null,
            ContactName: null,
            ContactPhone: null,
            RewardAmount: null,
            RewardNote: null);

        await _sut.Handle(command, CancellationToken.None);

        await _dispatcher.Received(1).DispatchVerifiedAllyAlertsAsync(
            Arg.Any<Guid>(),
            "Luna",
            nameof(PetSpecies.Cat),
            null,
            9.9281,
            -84.0907,
            Arg.Any<CancellationToken>());
    }
}

