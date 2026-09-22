using FluentAssertions;
using PawTrack.Domain.Collars;

namespace PawTrack.UnitTests.Collars;

public sealed class CollarConnectivityDomainTests
{
    private static Collar MakeCollar() =>
        Collar.Register(Guid.NewGuid(), Guid.NewGuid(), CollarProvider.Own, null);

    [Fact]
    public void Register_DefaultsToAlertsEnabledWithStandardThresholds()
    {
        var collar = MakeCollar();

        collar.OfflineAlertsEnabled.Should().BeTrue();
        collar.OfflineThresholdMinutes.Should().Be(120);
        collar.BatteryAlertsEnabled.Should().BeTrue();
        collar.BatteryAlertThresholdPercent.Should().Be(20);
        collar.IsOffline.Should().BeFalse();
    }

    [Fact]
    public void MarkOffline_SetsIsOfflineTrue()
    {
        var collar = MakeCollar();

        collar.MarkOffline();

        collar.IsOffline.Should().BeTrue();
    }

    [Fact]
    public void UpdateLocation_AfterMarkedOffline_ClearsOfflineFlag()
    {
        var collar = MakeCollar();
        collar.MarkOffline();

        collar.UpdateLocation(9.9, -84.1, 55, DateTimeOffset.UtcNow);

        collar.IsOffline.Should().BeFalse();
        collar.BatteryPercent.Should().Be(55);
    }

    [Fact]
    public void UpdateLocation_FirstPosition_IsAcceptedAndStoresDeviceTimestamp()
    {
        var collar = MakeCollar();
        var recordedAt = DateTimeOffset.UtcNow.AddMinutes(-1);

        var outcome = collar.UpdateLocation(9.9, -84.1, 70, recordedAt, isOnline: true);

        outcome.Accepted.Should().BeTrue();
        collar.LastLat.Should().Be(9.9);
        collar.LastLng.Should().Be(-84.1);
        collar.LastLocationRecordedAt.Should().Be(recordedAt);
        collar.LastPositionOnline.Should().BeTrue();
    }

    [Fact]
    public void UpdateLocation_NewerPositionThanLastAccepted_IsAccepted()
    {
        var collar = MakeCollar();
        var first = DateTimeOffset.UtcNow.AddMinutes(-10);
        var second = DateTimeOffset.UtcNow.AddMinutes(-5);
        collar.UpdateLocation(9.9, -84.1, 70, first);

        var outcome = collar.UpdateLocation(9.95, -84.15, 65, second);

        outcome.Accepted.Should().BeTrue();
        collar.LastLat.Should().Be(9.95);
        collar.LastLocationRecordedAt.Should().Be(second);
    }

    [Fact]
    public void UpdateLocation_OlderThanLastAccepted_IsRejectedAndDoesNotOverwriteState()
    {
        var collar = MakeCollar();
        var latest = DateTimeOffset.UtcNow.AddMinutes(-1);
        var stale = DateTimeOffset.UtcNow.AddMinutes(-30);
        collar.UpdateLocation(9.9, -84.1, 70, latest);

        var outcome = collar.UpdateLocation(1.0, 1.0, 10, stale);

        outcome.Accepted.Should().BeFalse();
        outcome.RejectionReason.Should().NotBeNullOrWhiteSpace();
        collar.LastLat.Should().Be(9.9);
        collar.LastLng.Should().Be(-84.1);
        collar.BatteryPercent.Should().Be(70);
        collar.LastLocationRecordedAt.Should().Be(latest);
    }

    [Fact]
    public void UpdateLocation_DuplicateTimestamp_IsRejectedAsNotNewer()
    {
        var collar = MakeCollar();
        var recordedAt = DateTimeOffset.UtcNow.AddMinutes(-2);
        collar.UpdateLocation(9.9, -84.1, 70, recordedAt);

        var outcome = collar.UpdateLocation(9.91, -84.11, 71, recordedAt);

        outcome.Accepted.Should().BeFalse();
        collar.LastLat.Should().Be(9.9);
    }

    [Fact]
    public void UpdateLocation_TimestampBeyondClockSkewTolerance_IsRejected()
    {
        var collar = MakeCollar();

        var outcome = collar.UpdateLocation(9.9, -84.1, 70, DateTimeOffset.UtcNow.AddMinutes(30));

        outcome.Accepted.Should().BeFalse();
        collar.LastLocationRecordedAt.Should().BeNull();
    }

    [Fact]
    public void UpdateLocation_RejectedPosition_LeavesIsOfflineUntouched()
    {
        var collar = MakeCollar();
        collar.MarkOffline();

        var outcome = collar.UpdateLocation(9.9, -84.1, 70, DateTimeOffset.UtcNow.AddMinutes(30));

        outcome.Accepted.Should().BeFalse();
        collar.IsOffline.Should().BeTrue();
    }

    [Fact]
    public void UpdateNotificationPreferences_ValidValues_UpdatesAllFields()
    {
        var collar = MakeCollar();

        var result = collar.UpdateNotificationPreferences(
            offlineAlertsEnabled: false, offlineThresholdMinutes: 60,
            batteryAlertsEnabled: false, batteryAlertThresholdPercent: 15);

        result.IsSuccess.Should().BeTrue();
        collar.OfflineAlertsEnabled.Should().BeFalse();
        collar.OfflineThresholdMinutes.Should().Be(60);
        collar.BatteryAlertsEnabled.Should().BeFalse();
        collar.BatteryAlertThresholdPercent.Should().Be(15);
    }

    [Theory]
    [InlineData(14)]   // below minimum
    [InlineData(1441)] // above maximum
    public void UpdateNotificationPreferences_InvalidOfflineThreshold_ReturnsFailure(int minutes)
    {
        var collar = MakeCollar();

        var result = collar.UpdateNotificationPreferences(true, minutes, true, 20);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainMatch("*umbral de desconexión*");
    }

    [Theory]
    [InlineData(4)]  // below minimum
    [InlineData(51)] // above maximum
    public void UpdateNotificationPreferences_InvalidBatteryThreshold_ReturnsFailure(int percent)
    {
        var collar = MakeCollar();

        var result = collar.UpdateNotificationPreferences(true, 120, true, percent);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainMatch("*umbral de batería*");
    }
}

public sealed class CollarLocationDomainTests
{
    [Fact]
    public void Record_StoresDeviceTimestampSeparatelyFromServerReceipt()
    {
        var collarId = Guid.NewGuid();
        var deviceRecordedAt = DateTimeOffset.UtcNow.AddMinutes(-7);
        var serverReceivedAt = DateTimeOffset.UtcNow;

        var point = CollarLocation.Record(
            collarId, 9.9, -84.1, deviceRecordedAt,
            accuracy: 12, isOnline: false, receivedAt: serverReceivedAt);

        point.RecordedAt.Should().Be(deviceRecordedAt);
        point.ReceivedAt.Should().Be(serverReceivedAt);
        point.IsOnline.Should().BeFalse();
        point.Accuracy.Should().Be(12);
    }

    [Fact]
    public void Record_WithoutExplicitReceivedAt_DefaultsToNow()
    {
        var before = DateTimeOffset.UtcNow;

        var point = CollarLocation.Record(Guid.NewGuid(), 9.9, -84.1, before);

        point.ReceivedAt.Should().BeOnOrAfter(before);
        point.IsOnline.Should().BeTrue();
    }
}
