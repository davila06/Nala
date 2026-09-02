using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using PawTrack.Application.Collars.Interfaces;
using PawTrack.Application.Collars.Services;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Collars;
using PawTrack.Domain.Notifications;
using PawTrack.Domain.Pets;

namespace PawTrack.UnitTests.Collars.Services;

public sealed class CollarSafeZoneEvaluationServiceTests
{
    private readonly ICollarSafeZoneRepository _safeZoneRepo = Substitute.For<ICollarSafeZoneRepository>();
    private readonly IPetRepository _petRepo = Substitute.For<IPetRepository>();
    private readonly INotificationRepository _notificationRepo = Substitute.For<INotificationRepository>();
    private readonly IPushNotificationService _pushService = Substitute.For<IPushNotificationService>();

    private readonly CollarSafeZoneEvaluationService _sut;

    private static readonly string SquarePolygonJson = JsonSerializer.Serialize(new[]
    {
        new { lat = 9.9, lng = -84.1 },
        new { lat = 9.9, lng = -84.0 },
        new { lat = 10.0, lng = -84.0 },
        new { lat = 10.0, lng = -84.1 },
    });

    public CollarSafeZoneEvaluationServiceTests()
    {
        _sut = new CollarSafeZoneEvaluationService(
            _safeZoneRepo, _petRepo, _notificationRepo, _pushService,
            Substitute.For<ILogger<CollarSafeZoneEvaluationService>>());
    }

    private static Collar MakeCollar()
    {
        var collar = Collar.Register(Guid.NewGuid(), Guid.NewGuid(), CollarProvider.Own, null);
        return collar;
    }

    [Fact]
    public async Task EvaluateAsync_NoZones_DoesNothing()
    {
        var collar = MakeCollar();
        _safeZoneRepo.GetEnabledByCollarIdAsync(collar.Id, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<CollarSafeZone>());

        await _sut.EvaluateAsync(collar, 9.95, -84.05, default);

        await _notificationRepo.DidNotReceive().AddAsync(Arg.Any<Notification>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EvaluateAsync_BreachDetected_SendsNotification()
    {
        var collar = MakeCollar();
        var zone = CollarSafeZone.Create(collar.Id, "Casa", SquarePolygonJson);
        zone.Evaluate(9.95, -84.05); // establish baseline: inside
        _safeZoneRepo.GetEnabledByCollarIdAsync(collar.Id, Arg.Any<CancellationToken>())
            .Returns(new[] { zone });
        _petRepo.GetByIdAsync(collar.PetId, Arg.Any<CancellationToken>())
            .Returns(Pet.Create(collar.OwnerId, "Fido", PetSpecies.Dog, null, null));

        await _sut.EvaluateAsync(collar, 8.0, -83.0, default); // now outside

        await _notificationRepo.Received(1).AddAsync(
            Arg.Is<Notification>(n => n.Type == NotificationType.CollarSafeZoneBreach),
            Arg.Any<CancellationToken>());
        await _pushService.Received(1).SendAsync(
            collar.OwnerId, Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<PushNotificationMetadata?>(), Arg.Any<CancellationToken>());
        _safeZoneRepo.Received(1).Update(zone);
    }

    [Fact]
    public async Task EvaluateAsync_StillInside_DoesNotNotify()
    {
        var collar = MakeCollar();
        var zone = CollarSafeZone.Create(collar.Id, "Casa", SquarePolygonJson);
        zone.Evaluate(9.95, -84.05);
        _safeZoneRepo.GetEnabledByCollarIdAsync(collar.Id, Arg.Any<CancellationToken>())
            .Returns(new[] { zone });

        await _sut.EvaluateAsync(collar, 9.96, -84.06, default);

        await _notificationRepo.DidNotReceive().AddAsync(Arg.Any<Notification>(), Arg.Any<CancellationToken>());
    }
}
