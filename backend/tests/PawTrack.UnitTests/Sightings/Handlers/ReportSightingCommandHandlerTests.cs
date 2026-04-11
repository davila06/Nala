using FluentAssertions;
using NSubstitute;
using PawTrack.Application.Common;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Sightings.Commands.ReportSighting;
using PawTrack.Domain.LostPets;
using PawTrack.Domain.Locations;
using PawTrack.Domain.Notifications;
using PawTrack.Domain.Pets;
using Microsoft.Extensions.Options;
using PawTrack.Application.Common.Settings;

namespace PawTrack.UnitTests.Sightings.Handlers;

public sealed class ReportSightingCommandHandlerTests
{
    private readonly ISightingRepository _sightingRepo = Substitute.For<ISightingRepository>();
    private readonly IPetRepository _petRepo = Substitute.For<IPetRepository>();
    private readonly ILostPetRepository _lostPetRepo = Substitute.For<ILostPetRepository>();
    private readonly IUserRepository _userRepo = Substitute.For<IUserRepository>();
    private readonly IUserLocationRepository _userLocationRepo = Substitute.For<IUserLocationRepository>();
    private readonly INotificationRepository _notificationRepo = Substitute.For<INotificationRepository>();
    private readonly IBlobStorageService _blobService = Substitute.For<IBlobStorageService>();
    private readonly IImageProcessor _imageProcessor = Substitute.For<IImageProcessor>();
    private readonly IPiiScrubber _piiScrubber = Substitute.For<IPiiScrubber>();
    private readonly INotificationDispatcher _dispatcher = Substitute.For<INotificationDispatcher>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    private readonly ReportSightingCommandHandler _sut;

    public ReportSightingCommandHandlerTests()
    {
        _sut = new ReportSightingCommandHandler(
            _sightingRepo, _petRepo, _lostPetRepo, _userRepo,
            _userLocationRepo, _notificationRepo,
            _blobService, _imageProcessor, _piiScrubber, _dispatcher,
            Options.Create(new ResolveCheckSettings()),
            _uow);
    }

    [Fact]
    public async Task Handle_ValidSightingNoPhoto_CreatesRecord()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var pet = Pet.Create(ownerId, "Mochi", PetSpecies.Cat, null, null);

        _petRepo.GetByIdAsync(pet.Id, Arg.Any<CancellationToken>()).Returns(pet);
        _lostPetRepo.GetActiveByPetIdAsync(pet.Id, Arg.Any<CancellationToken>())
            .Returns((LostPetEvent?)null);
        _piiScrubber.Scrub(Arg.Any<string?>()).Returns(x => x.ArgAt<string?>(0));
        _uow.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

        var command = new ReportSightingCommand(
            pet.Id, 9.9281, -84.0907,
            "Gray cat near central park", null, null,
            DateTimeOffset.UtcNow.AddMinutes(-30));

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNullOrEmpty();

        await _sightingRepo.Received(1).AddAsync(Arg.Any<Domain.Sightings.Sighting>(), Arg.Any<CancellationToken>());
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());

        // No active lost report → no notification dispatched
        await _dispatcher.DidNotReceive().DispatchSightingAlertAsync(
            Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_PetNotFound_ReturnsFailure()
    {
        _petRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((Pet?)null);

        var command = new ReportSightingCommand(
            Guid.NewGuid(), 9.9, -84.0, null, null, null, DateTimeOffset.UtcNow);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain("Pet not found.");
    }

    [Fact]
    public async Task Handle_ActiveLostReport_DispatchesSightingAlert()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var pet = Pet.Create(ownerId, "Rex", PetSpecies.Dog, null, null);
        pet.MarkAsLost();

        var lostReport = LostPetEvent.Create(
            pet.Id, ownerId, "Lost near park", 9.9, -84.0, DateTimeOffset.UtcNow.AddDays(-1));

        var (owner, _) = Domain.Auth.User.Create("owner@test.com", "Rex Owner", Guid.NewGuid().ToString("N"));

        _petRepo.GetByIdAsync(pet.Id, Arg.Any<CancellationToken>()).Returns(pet);
        _lostPetRepo.GetActiveByPetIdAsync(pet.Id, Arg.Any<CancellationToken>()).Returns(lostReport);
        _userRepo.GetByIdAsync(ownerId, Arg.Any<CancellationToken>()).Returns(owner);
        _piiScrubber.Scrub(Arg.Any<string?>()).Returns(x => x.ArgAt<string?>(0));
        _uow.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

        var command = new ReportSightingCommand(
            pet.Id, 9.9281, -84.0907, "Dog running near highway",
            null, null, DateTimeOffset.UtcNow.AddMinutes(-10));

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _dispatcher.Received(1).DispatchSightingAlertAsync(
            owner.Id, owner.Email, owner.Name, pet.Name,
            Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_PiiScrubberCalledOnNote()
    {
        var ownerId = Guid.NewGuid();
        var pet = Pet.Create(ownerId, "Bella", PetSpecies.Dog, null, null);
        const string rawNote = "Call 8888-1234";
        const string scrubbedNote = "Call [REDACTED]";

        _petRepo.GetByIdAsync(pet.Id, Arg.Any<CancellationToken>()).Returns(pet);
        _lostPetRepo.GetActiveByPetIdAsync(pet.Id, Arg.Any<CancellationToken>())
            .Returns((LostPetEvent?)null);
        _piiScrubber.Scrub(rawNote).Returns(scrubbedNote);
        _uow.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

        var command = new ReportSightingCommand(
            pet.Id, 9.9, -84.0, rawNote, null, null, DateTimeOffset.UtcNow);

        await _sut.Handle(command, CancellationToken.None);

        _piiScrubber.Received(1).Scrub(rawNote);
    }

    [Fact]
    public async Task Handle_ActiveLostReportAndSightingNearOwnerHome_CreatesResolveCheckNotification()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var pet = Pet.Create(ownerId, "Kira", PetSpecies.Dog, null, null);
        pet.MarkAsLost();

        var lostReport = LostPetEvent.Create(
            pet.Id,
            ownerId,
            "Missing",
            9.9300,
            -84.0800,
            DateTimeOffset.UtcNow.AddHours(-8));

        var (owner, _) = Domain.Auth.User.Create("owner@test.com", "Owner Name", Guid.NewGuid().ToString("N"));

        _petRepo.GetByIdAsync(pet.Id, Arg.Any<CancellationToken>()).Returns(pet);
        _lostPetRepo.GetActiveByPetIdAsync(pet.Id, Arg.Any<CancellationToken>()).Returns(lostReport);
        _userRepo.GetByIdAsync(ownerId, Arg.Any<CancellationToken>()).Returns(owner);
        _userLocationRepo.GetByUserIdAsync(ownerId, Arg.Any<CancellationToken>())
            .Returns(UserLocation.Create(ownerId, 9.9301, -84.0801, true));
        _notificationRepo.HasRecentByUserTypeAndEntityAsync(
                ownerId,
                NotificationType.ResolveCheck,
                lostReport.Id.ToString(),
                Arg.Any<TimeSpan>(),
                Arg.Any<CancellationToken>())
            .Returns(false);

        _piiScrubber.Scrub(Arg.Any<string?>()).Returns(x => x.ArgAt<string?>(0));

        var command = new ReportSightingCommand(
            pet.Id,
            9.9302,
            -84.0802,
            "Near home",
            null,
            null,
            DateTimeOffset.UtcNow);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        await _notificationRepo.Received(1).AddAsync(
            Arg.Is<Notification>(n =>
                n.UserId == ownerId
                && n.Type == NotificationType.ResolveCheck
                && n.RelatedEntityId == lostReport.Id.ToString()),
            Arg.Any<CancellationToken>());
    }
}
