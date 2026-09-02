using System.Text.Json;
using FluentAssertions;
using NSubstitute;
using PawTrack.Application.Collars.Interfaces;
using PawTrack.Application.Collars.Queries.GetCollarSafeZones;
using PawTrack.Domain.Collars;

namespace PawTrack.UnitTests.Collars.Handlers;

public sealed class GetCollarSafeZonesQueryHandlerTests
{
    private readonly ICollarRepository _collarRepo = Substitute.For<ICollarRepository>();
    private readonly ICollarSafeZoneRepository _safeZoneRepo = Substitute.For<ICollarSafeZoneRepository>();
    private readonly GetCollarSafeZonesQueryHandler _sut;
    private static readonly Guid OwnerId = Guid.NewGuid();
    private static readonly Guid CollarId = Guid.NewGuid();

    private static readonly string ValidPolygonJson = JsonSerializer.Serialize(new[]
    {
        new { lat = 9.9, lng = -84.1 },
        new { lat = 9.9, lng = -84.0 },
        new { lat = 10.0, lng = -84.0 },
    });

    public GetCollarSafeZonesQueryHandlerTests()
    {
        _sut = new GetCollarSafeZonesQueryHandler(_collarRepo, _safeZoneRepo);
    }

    [Fact]
    public async Task Handle_Owner_ReturnsZones()
    {
        var collar = Collar.Register(Guid.NewGuid(), OwnerId, CollarProvider.Own, null);
        typeof(Collar).GetProperty("Id")!.SetValue(collar, CollarId);
        var zone = CollarSafeZone.Create(CollarId, "Casa", ValidPolygonJson);
        _collarRepo.GetByIdAsync(CollarId, Arg.Any<CancellationToken>()).Returns(collar);
        _safeZoneRepo.GetByCollarIdAsync(CollarId, Arg.Any<CancellationToken>()).Returns(new[] { zone });

        var result = await _sut.Handle(new GetCollarSafeZonesQuery(CollarId, OwnerId), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle(z => z.Name == "Casa");
    }

    [Fact]
    public async Task Handle_WrongOwner_ReturnsAccessDenied()
    {
        var collar = Collar.Register(Guid.NewGuid(), Guid.NewGuid(), CollarProvider.Own, null);
        typeof(Collar).GetProperty("Id")!.SetValue(collar, CollarId);
        _collarRepo.GetByIdAsync(CollarId, Arg.Any<CancellationToken>()).Returns(collar);

        var result = await _sut.Handle(new GetCollarSafeZonesQuery(CollarId, OwnerId), default);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain("Access denied.");
    }
}
