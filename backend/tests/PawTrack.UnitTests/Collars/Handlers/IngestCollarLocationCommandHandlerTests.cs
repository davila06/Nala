using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using PawTrack.Application.Collars.Commands.IngestCollarLocation;
using PawTrack.Application.Collars.Interfaces;
using PawTrack.Application.Collars.Services;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Collars;

namespace PawTrack.UnitTests.Collars.Handlers;

public sealed class IngestCollarLocationCommandHandlerTests
{
    private readonly ICollarTagRepository _tagRepo = Substitute.For<ICollarTagRepository>();
    private readonly ICollarRepository _collarRepo = Substitute.For<ICollarRepository>();
    private readonly ICollarAuditRepository _auditRepo = Substitute.For<ICollarAuditRepository>();
    private readonly ILostPetRepository _lostPetRepo = Substitute.For<ILostPetRepository>();
    private readonly ICollarSafeZoneRepository _safeZoneRepo = Substitute.For<ICollarSafeZoneRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    private readonly IngestCollarLocationCommandHandler _sut;

    private static readonly Guid CollarId = Guid.NewGuid();
    private const string Serial = "PT-A3F9-0001234";

    public IngestCollarLocationCommandHandlerTests()
    {
        _safeZoneRepo.GetEnabledByCollarIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<CollarSafeZone>());
        var safeZoneEvaluationService = new CollarSafeZoneEvaluationService(
            _safeZoneRepo, Substitute.For<IPetRepository>(), Substitute.For<INotificationRepository>(),
            Substitute.For<IPushNotificationService>(), NullLogger<CollarSafeZoneEvaluationService>.Instance);
        _sut = new IngestCollarLocationCommandHandler(
            _tagRepo, _collarRepo, _auditRepo, _lostPetRepo, safeZoneEvaluationService, _uow);
    }

    private Collar MakeActiveCollar()
    {
        var collar = Collar.Register(Guid.NewGuid(), Guid.NewGuid(), CollarProvider.Own, null);
        typeof(Collar).GetProperty("Id")!.SetValue(collar, CollarId);
        return collar;
    }

    private CollarTag MakeActivatedTag()
    {
        var tag = CollarTag.CreateFromFactory(Serial, "1.0.0");
        tag.Activate(CollarId);
        typeof(CollarTag).GetProperty("CollarId")!.SetValue(tag, CollarId);
        return tag;
    }

    [Fact]
    public async Task Handle_ValidIngest_UpdatesCollarAndRecordsLocation()
    {
        var collar = MakeActiveCollar();
        var tag = MakeActivatedTag();
        _tagRepo.GetBySerialAsync(Serial, Arg.Any<CancellationToken>()).Returns(tag);
        _collarRepo.GetByIdAsync(CollarId, Arg.Any<CancellationToken>()).Returns(collar);

        var cmd = new IngestCollarLocationCommand(CollarId, Serial, 9.9, -84.1, 85, DateTimeOffset.UtcNow, 5);
        var result = await _sut.Handle(cmd, default);

        result.IsSuccess.Should().BeTrue();
        collar.LastLat.Should().Be(9.9);
        collar.LastLng.Should().Be(-84.1);
        collar.BatteryPercent.Should().Be(85);
        await _collarRepo.Received(1).AddLocationAsync(Arg.Any<CollarLocation>(), default);
        await _uow.Received(1).SaveChangesAsync(default);
    }

    [Fact]
    public async Task Handle_LostModeCollar_SyncsLostPetEventLastSeenLocation()
    {
        var collar = MakeActiveCollar();
        var lostPetEventId = Guid.NewGuid();
        collar.ActivateLostMode(lostPetEventId);
        var tag = MakeActivatedTag();
        var lostPetEvent = PawTrack.Domain.LostPets.LostPetEvent.Create(
            collar.PetId, collar.OwnerId, null, null, null, DateTimeOffset.UtcNow.AddHours(-1));
        _tagRepo.GetBySerialAsync(Serial, Arg.Any<CancellationToken>()).Returns(tag);
        _collarRepo.GetByIdAsync(CollarId, Arg.Any<CancellationToken>()).Returns(collar);
        _lostPetRepo.GetByIdAsync(lostPetEventId, Arg.Any<CancellationToken>()).Returns(lostPetEvent);

        var timestamp = DateTimeOffset.UtcNow;
        var cmd = new IngestCollarLocationCommand(CollarId, Serial, 9.5, -84.2, 60, timestamp, 5);
        var result = await _sut.Handle(cmd, default);

        result.IsSuccess.Should().BeTrue();
        lostPetEvent.LastSeenLat.Should().Be(9.5);
        lostPetEvent.LastSeenLng.Should().Be(-84.2);
        _lostPetRepo.Received(1).Update(lostPetEvent);
    }

    [Fact]
    public async Task Handle_SerialMismatch_ReturnsFailure()
    {
        var tag = MakeActivatedTag();
        // Tag is linked to CollarId, but command comes in with a different CollarId
        var differentCollarId = Guid.NewGuid();
        _tagRepo.GetBySerialAsync(Serial, Arg.Any<CancellationToken>()).Returns(tag);

        var cmd = new IngestCollarLocationCommand(differentCollarId, Serial, 9.9, -84.1, null, DateTimeOffset.UtcNow, null);
        var result = await _sut.Handle(cmd, default);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainMatch("*mismatch*");
        await _auditRepo.Received(1).AddAsync(
            Arg.Is<CollarAuditEntry>(e => e.Event == CollarAuditEvent.LocationIngestFailed),
            Arg.Any<CancellationToken>());
        await _uow.Received(1).SaveChangesAsync(default);
    }

    [Fact]
    public async Task Handle_InactiveCollar_ReturnsFailure()
    {
        var collar = MakeActiveCollar();
        collar.Deactivate();
        var tag = MakeActivatedTag();
        _tagRepo.GetBySerialAsync(Serial, Arg.Any<CancellationToken>()).Returns(tag);
        _collarRepo.GetByIdAsync(CollarId, Arg.Any<CancellationToken>()).Returns(collar);

        var cmd = new IngestCollarLocationCommand(CollarId, Serial, 9.9, -84.1, null, DateTimeOffset.UtcNow, null);
        var result = await _sut.Handle(cmd, default);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainMatch("*inactivo*");
    }
}
