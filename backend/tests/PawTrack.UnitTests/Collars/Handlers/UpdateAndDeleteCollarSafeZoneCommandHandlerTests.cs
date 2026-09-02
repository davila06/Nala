using System.Text.Json;
using FluentAssertions;
using NSubstitute;
using PawTrack.Application.Collars.Commands.DeleteCollarSafeZone;
using PawTrack.Application.Collars.Commands.UpdateCollarSafeZone;
using PawTrack.Application.Collars.Interfaces;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Collars;

namespace PawTrack.UnitTests.Collars.Handlers;

public sealed class UpdateAndDeleteCollarSafeZoneCommandHandlerTests
{
    private readonly ICollarSafeZoneRepository _safeZoneRepo = Substitute.For<ICollarSafeZoneRepository>();
    private readonly ICollarRepository _collarRepo = Substitute.For<ICollarRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    private static readonly Guid OwnerId = Guid.NewGuid();
    private static readonly Guid CollarId = Guid.NewGuid();
    private static readonly Guid ZoneId = Guid.NewGuid();

    private static readonly string ValidPolygonJson = JsonSerializer.Serialize(new[]
    {
        new { lat = 9.9, lng = -84.1 },
        new { lat = 9.9, lng = -84.0 },
        new { lat = 10.0, lng = -84.0 },
    });

    private static CollarSafeZone MakeZone()
    {
        var zone = CollarSafeZone.Create(CollarId, "Casa", ValidPolygonJson);
        typeof(CollarSafeZone).GetProperty("Id")!.SetValue(zone, ZoneId);
        return zone;
    }

    private static Collar MakeCollar(Guid ownerId)
    {
        var collar = Collar.Register(Guid.NewGuid(), ownerId, CollarProvider.Own, null);
        typeof(Collar).GetProperty("Id")!.SetValue(collar, CollarId);
        return collar;
    }

    [Fact]
    public async Task Update_HappyPath_UpdatesZone()
    {
        var sut = new UpdateCollarSafeZoneCommandHandler(_safeZoneRepo, _collarRepo, _uow);
        var zone = MakeZone();
        var collar = MakeCollar(OwnerId);
        _safeZoneRepo.GetByIdAsync(ZoneId, Arg.Any<CancellationToken>()).Returns(zone);
        _collarRepo.GetByIdAsync(CollarId, Arg.Any<CancellationToken>()).Returns(collar);

        var result = await sut.Handle(
            new UpdateCollarSafeZoneCommand(ZoneId, OwnerId, "Parque", ValidPolygonJson, false), default);

        result.IsSuccess.Should().BeTrue();
        zone.Name.Should().Be("Parque");
        zone.Enabled.Should().BeFalse();
    }

    [Fact]
    public async Task Update_WrongOwner_ReturnsAccessDenied()
    {
        var sut = new UpdateCollarSafeZoneCommandHandler(_safeZoneRepo, _collarRepo, _uow);
        var zone = MakeZone();
        var collar = MakeCollar(Guid.NewGuid());
        _safeZoneRepo.GetByIdAsync(ZoneId, Arg.Any<CancellationToken>()).Returns(zone);
        _collarRepo.GetByIdAsync(CollarId, Arg.Any<CancellationToken>()).Returns(collar);

        var result = await sut.Handle(
            new UpdateCollarSafeZoneCommand(ZoneId, OwnerId, "Parque", ValidPolygonJson, false), default);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain("Access denied.");
    }

    [Fact]
    public async Task Delete_HappyPath_RemovesZone()
    {
        var sut = new DeleteCollarSafeZoneCommandHandler(_safeZoneRepo, _collarRepo, _uow);
        var zone = MakeZone();
        var collar = MakeCollar(OwnerId);
        _safeZoneRepo.GetByIdAsync(ZoneId, Arg.Any<CancellationToken>()).Returns(zone);
        _collarRepo.GetByIdAsync(CollarId, Arg.Any<CancellationToken>()).Returns(collar);

        var result = await sut.Handle(new DeleteCollarSafeZoneCommand(ZoneId, OwnerId), default);

        result.IsSuccess.Should().BeTrue();
        _safeZoneRepo.Received(1).Remove(zone);
    }

    [Fact]
    public async Task Delete_ZoneNotFound_ReturnsFailure()
    {
        var sut = new DeleteCollarSafeZoneCommandHandler(_safeZoneRepo, _collarRepo, _uow);
        _safeZoneRepo.GetByIdAsync(ZoneId, Arg.Any<CancellationToken>()).Returns((CollarSafeZone?)null);

        var result = await sut.Handle(new DeleteCollarSafeZoneCommand(ZoneId, OwnerId), default);

        result.IsFailure.Should().BeTrue();
    }
}
