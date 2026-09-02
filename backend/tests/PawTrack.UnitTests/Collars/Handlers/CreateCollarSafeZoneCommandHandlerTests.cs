using System.Text.Json;
using FluentAssertions;
using NSubstitute;
using PawTrack.Application.Collars.Commands.CreateCollarSafeZone;
using PawTrack.Application.Collars.Interfaces;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Collars;

namespace PawTrack.UnitTests.Collars.Handlers;

public sealed class CreateCollarSafeZoneCommandHandlerTests
{
    private readonly ICollarRepository _collarRepo = Substitute.For<ICollarRepository>();
    private readonly ICollarSafeZoneRepository _safeZoneRepo = Substitute.For<ICollarSafeZoneRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    private readonly CreateCollarSafeZoneCommandHandler _sut;
    private static readonly Guid OwnerId = Guid.NewGuid();
    private static readonly Guid CollarId = Guid.NewGuid();

    private static readonly string ValidPolygonJson = JsonSerializer.Serialize(new[]
    {
        new { lat = 9.9, lng = -84.1 },
        new { lat = 9.9, lng = -84.0 },
        new { lat = 10.0, lng = -84.0 },
    });

    public CreateCollarSafeZoneCommandHandlerTests()
    {
        _sut = new CreateCollarSafeZoneCommandHandler(_collarRepo, _safeZoneRepo, _uow);
    }

    private static Collar MakeCollar(Guid ownerId)
    {
        var collar = Collar.Register(Guid.NewGuid(), ownerId, CollarProvider.Own, null);
        typeof(Collar).GetProperty("Id")!.SetValue(collar, CollarId);
        return collar;
    }

    [Fact]
    public async Task Handle_HappyPath_CreatesZone()
    {
        var collar = MakeCollar(OwnerId);
        _collarRepo.GetByIdAsync(CollarId, Arg.Any<CancellationToken>()).Returns(collar);

        var result = await _sut.Handle(
            new CreateCollarSafeZoneCommand(CollarId, OwnerId, "Casa", ValidPolygonJson), default);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Casa");
        await _safeZoneRepo.Received(1).AddAsync(Arg.Any<CollarSafeZone>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_InvalidPolygon_ReturnsFailure()
    {
        var collar = MakeCollar(OwnerId);
        _collarRepo.GetByIdAsync(CollarId, Arg.Any<CancellationToken>()).Returns(collar);

        var result = await _sut.Handle(
            new CreateCollarSafeZoneCommand(CollarId, OwnerId, "Casa", "not json"), default);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WrongOwner_ReturnsAccessDenied()
    {
        var collar = MakeCollar(Guid.NewGuid());
        _collarRepo.GetByIdAsync(CollarId, Arg.Any<CancellationToken>()).Returns(collar);

        var result = await _sut.Handle(
            new CreateCollarSafeZoneCommand(CollarId, OwnerId, "Casa", ValidPolygonJson), default);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain("Access denied.");
    }
}
