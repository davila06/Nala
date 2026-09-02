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

        collar.UpdateLocation(9.9, -84.1, 55);

        collar.IsOffline.Should().BeFalse();
        collar.BatteryPercent.Should().Be(55);
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
