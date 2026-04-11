using FluentAssertions;
using NSubstitute;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Pets.Commands.RecordPublicQrScan;
using PawTrack.Application.Pets.Queries.GetPetScanHistory;
using PawTrack.Domain.Notifications;
using PawTrack.Domain.Pets;
using Microsoft.Extensions.Options;
using PawTrack.Application.Common.Settings;

namespace PawTrack.UnitTests.Pets.Handlers;

public sealed class RecordPublicQrScanCommandHandlerTests
{
    private readonly IPetRepository _petRepository = Substitute.For<IPetRepository>();
    private readonly IQrScanEventRepository _qrScanRepository = Substitute.For<IQrScanEventRepository>();
    private readonly ILostPetRepository _lostPetRepository = Substitute.For<ILostPetRepository>();
    private readonly IUserLocationRepository _userLocationRepository = Substitute.For<IUserLocationRepository>();
    private readonly INotificationRepository _notificationRepository = Substitute.For<INotificationRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private static IOptions<ResolveCheckSettings> DefaultSettings() =>
        Options.Create(new ResolveCheckSettings());


    [Fact]
    public async Task Handle_MissingPet_ReturnsFailure()
    {
        _petRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Pet?)null);

        var sut = new RecordPublicQrScanCommandHandler(
            _petRepository,
            _qrScanRepository,
            _lostPetRepository,
            _userLocationRepository,
            _notificationRepository,
            DefaultSettings(),
            _unitOfWork);

        var result = await sut.Handle(
            new RecordPublicQrScanCommand(
                Guid.NewGuid(),
                null,
                "1.2.3.4",
                "Mozilla/5.0",
                "CR",
                "San Jose",
                DateTimeOffset.UtcNow,
                null,
                null),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain("Pet not found.");
    }

    [Fact]
    public async Task Handle_LostPetFirstDailyScan_CreatesScanAndNotification()
    {
        var ownerId = Guid.NewGuid();
        var pet = Pet.Create(ownerId, "Nala", PetSpecies.Dog, null, null);
        pet.MarkAsLost();

        _petRepository.GetByIdAsync(pet.Id, Arg.Any<CancellationToken>()).Returns(pet);
        _qrScanRepository.HasScanForPetOnDateAsync(pet.Id, Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns(false);
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

        var sut = new RecordPublicQrScanCommandHandler(
            _petRepository,
            _qrScanRepository,
            _lostPetRepository,
            _userLocationRepository,
            _notificationRepository,
            DefaultSettings(),
            _unitOfWork);

        var scanAt = new DateTimeOffset(2026, 4, 5, 16, 25, 0, TimeSpan.Zero);
        var result = await sut.Handle(
            new RecordPublicQrScanCommand(
                pet.Id,
                null,
                "201.200.1.9",
                "Mozilla/5.0 (iPhone)",
                "CR",
                "Cartago",
                scanAt,
                null,
                null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        await _qrScanRepository.Received(1)
            .AddAsync(Arg.Is<QrScanEvent>(e =>
                e.PetId == pet.Id
                && e.CountryCode == "CR"
                && e.CityName == "Cartago"
                && e.UserAgent!.Contains("iPhone")
                && e.IpAddress != "201.200.1.9"
                && e.ScannedAt == scanAt), Arg.Any<CancellationToken>());

        await _notificationRepository.Received(1)
            .AddAsync(Arg.Is<Notification>(n =>
                n.UserId == ownerId
                && n.Type == NotificationType.SystemMessage
                && n.Title.Contains("QR")), Arg.Any<CancellationToken>());

        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetPetScanHistory_OwnerReceivesTodayCountAndOrderedEvents()
    {
        var ownerId = Guid.NewGuid();
        var pet = Pet.Create(ownerId, "Luna", PetSpecies.Cat, null, null);

        var older = QrScanEvent.Create(
            pet.Id,
            null,
            "hash-old",
            "Mozilla",
            "CR",
            "Heredia",
            DateTimeOffset.UtcNow.AddDays(-1));

        var newer = QrScanEvent.Create(
            pet.Id,
            null,
            "hash-new",
            "Safari",
            "CR",
            "San Jose",
            DateTimeOffset.UtcNow);

        _petRepository.GetByIdAsync(pet.Id, Arg.Any<CancellationToken>()).Returns(pet);
        _qrScanRepository.GetByPetIdAsync(pet.Id, 50, Arg.Any<CancellationToken>())
            .Returns([older, newer]);

        var sut = new GetPetScanHistoryQueryHandler(
            _petRepository,
            _qrScanRepository,
            Options.Create(new PetScanExportSettings()));

        var result = await sut.Handle(
            new GetPetScanHistoryQuery(pet.Id, ownerId),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.ScansToday.Should().Be(1);
        result.Value.Events.Should().HaveCount(2);
        result.Value.Events.First().CityName.Should().Be("San Jose");
    }
}
