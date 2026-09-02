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

public sealed class CollarConnectivityAlertServiceTests
{
    private readonly ICollarRepository _collarRepo = Substitute.For<ICollarRepository>();
    private readonly IPetRepository _petRepo = Substitute.For<IPetRepository>();
    private readonly INotificationRepository _notificationRepo = Substitute.For<INotificationRepository>();
    private readonly IPushNotificationService _pushService = Substitute.For<IPushNotificationService>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    private readonly CollarConnectivityAlertService _sut;

    public CollarConnectivityAlertServiceTests()
    {
        _sut = new CollarConnectivityAlertService(
            _collarRepo, _petRepo, _notificationRepo, _pushService, _uow,
            Substitute.For<ILogger<CollarConnectivityAlertService>>());
    }

    private static Collar MakeCollar(bool offlineEnabled = true, bool batteryEnabled = true)
    {
        var collar = Collar.Register(Guid.NewGuid(), Guid.NewGuid(), CollarProvider.Own, null);
        collar.UpdateNotificationPreferences(offlineEnabled, 120, batteryEnabled, 20);
        return collar;
    }

    private static Pet MakePet() => Pet.Create(Guid.NewGuid(), "Fido", PetSpecies.Dog, null, null);

    // ── Offline detection ─────────────────────────────────────────────────────

    [Fact]
    public async Task RunOfflineDetection_LastSeenBeyondThreshold_MarksOfflineAndNotifies()
    {
        var collar = MakeCollar();
        collar.UpdateLocation(9.9, -84.1, 50);
        typeof(Collar).GetProperty("LastSeenAt")!.SetValue(collar, DateTimeOffset.UtcNow.AddHours(-3));
        _collarRepo.GetActiveCollarsWithAlertsEnabledAsync(Arg.Any<CancellationToken>())
            .Returns(new[] { collar });
        _petRepo.GetByIdAsync(collar.PetId, Arg.Any<CancellationToken>()).Returns(MakePet());
        _notificationRepo.HasRecentByUserTypeAndEntityAsync(
                collar.OwnerId, NotificationType.CollarOfflineAlert, collar.Id.ToString(),
                Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(false);

        await _sut.RunOfflineDetectionAsync(default);

        collar.IsOffline.Should().BeTrue();
        await _notificationRepo.Received(1).AddAsync(Arg.Any<Notification>(), Arg.Any<CancellationToken>());
        await _pushService.Received(1).SendAsync(
            collar.OwnerId, Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<PushNotificationMetadata?>(), Arg.Any<CancellationToken>());
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunOfflineDetection_RecentAlertAlreadySent_DoesNotDuplicateNotification()
    {
        var collar = MakeCollar();
        collar.UpdateLocation(9.9, -84.1, 50);
        typeof(Collar).GetProperty("LastSeenAt")!.SetValue(collar, DateTimeOffset.UtcNow.AddHours(-3));
        _collarRepo.GetActiveCollarsWithAlertsEnabledAsync(Arg.Any<CancellationToken>())
            .Returns(new[] { collar });
        _notificationRepo.HasRecentByUserTypeAndEntityAsync(
                collar.OwnerId, NotificationType.CollarOfflineAlert, collar.Id.ToString(),
                Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(true);

        await _sut.RunOfflineDetectionAsync(default);

        collar.IsOffline.Should().BeTrue("collar is still marked offline even if the notification is throttled");
        await _notificationRepo.DidNotReceive().AddAsync(Arg.Any<Notification>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunOfflineDetection_WithinThreshold_DoesNothing()
    {
        var collar = MakeCollar();
        collar.UpdateLocation(9.9, -84.1, 50); // LastSeenAt = now
        _collarRepo.GetActiveCollarsWithAlertsEnabledAsync(Arg.Any<CancellationToken>())
            .Returns(new[] { collar });

        await _sut.RunOfflineDetectionAsync(default);

        collar.IsOffline.Should().BeFalse();
        await _uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunOfflineDetection_AlertsDisabled_Skips()
    {
        var collar = MakeCollar(offlineEnabled: false);
        typeof(Collar).GetProperty("LastSeenAt")!.SetValue(collar, DateTimeOffset.UtcNow.AddHours(-5));
        _collarRepo.GetActiveCollarsWithAlertsEnabledAsync(Arg.Any<CancellationToken>())
            .Returns(new[] { collar });

        await _sut.RunOfflineDetectionAsync(default);

        collar.IsOffline.Should().BeFalse();
    }

    // ── Battery alerts ─────────────────────────────────────────────────────────

    [Fact]
    public async Task RunBatteryAlertDetection_BelowThreshold_SendsAlert()
    {
        var collar = MakeCollar();
        collar.UpdateLocation(9.9, -84.1, 15); // below default 20% threshold
        _collarRepo.GetActiveCollarsWithAlertsEnabledAsync(Arg.Any<CancellationToken>())
            .Returns(new[] { collar });
        _petRepo.GetByIdAsync(collar.PetId, Arg.Any<CancellationToken>()).Returns(MakePet());
        _notificationRepo.HasRecentByUserTypeAndEntityAsync(
                collar.OwnerId, NotificationType.CollarLowBatteryAlert, collar.Id.ToString(),
                Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(false);

        await _sut.RunBatteryAlertDetectionAsync(default);

        await _notificationRepo.Received(1).AddAsync(Arg.Any<Notification>(), Arg.Any<CancellationToken>());
        await _pushService.Received(1).SendAsync(
            collar.OwnerId, Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<PushNotificationMetadata?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunBatteryAlertDetection_AboveThreshold_DoesNothing()
    {
        var collar = MakeCollar();
        collar.UpdateLocation(9.9, -84.1, 80);
        _collarRepo.GetActiveCollarsWithAlertsEnabledAsync(Arg.Any<CancellationToken>())
            .Returns(new[] { collar });

        await _sut.RunBatteryAlertDetectionAsync(default);

        await _notificationRepo.DidNotReceive().AddAsync(Arg.Any<Notification>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunBatteryAlertDetection_AlreadyAlertedWithinCooldown_DoesNotDuplicate()
    {
        var collar = MakeCollar();
        collar.UpdateLocation(9.9, -84.1, 10);
        _collarRepo.GetActiveCollarsWithAlertsEnabledAsync(Arg.Any<CancellationToken>())
            .Returns(new[] { collar });
        _notificationRepo.HasRecentByUserTypeAndEntityAsync(
                collar.OwnerId, NotificationType.CollarLowBatteryAlert, collar.Id.ToString(),
                Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(true);

        await _sut.RunBatteryAlertDetectionAsync(default);

        await _notificationRepo.DidNotReceive().AddAsync(Arg.Any<Notification>(), Arg.Any<CancellationToken>());
    }
}
