using FluentAssertions;
using PawTrack.API.Hubs;

namespace PawTrack.UnitTests.Hubs;

/// <summary>
/// Round-9 security: GPS coordinates received by the SignalR hub must be validated
/// before broadcasting to search participants. NaN, Infinity, and out-of-range values
/// must be silently rejected.
/// </summary>
public sealed class SearchCoordinationHubCoordinateValidationTests
{
    // ── Valid coordinates ─────────────────────────────────────────────────────

    [Theory]
    [InlineData(0.0, 0.0)]
    [InlineData(9.9281, -84.0907)]   // San José, Costa Rica
    [InlineData(-90.0, -180.0)]      // South-west extreme
    [InlineData(90.0, 180.0)]        // North-east extreme
    public void IsValidCoordinate_ValidValues_ReturnsTrue(double lat, double lng)
    {
        SearchCoordinationHub.IsValidCoordinate(lat, lng).Should().BeTrue();
    }

    // ── NaN / Infinity (pathological values) ─────────────────────────────────

    [Fact]
    public void IsValidCoordinate_NaN_ReturnsFalse()
    {
        SearchCoordinationHub.IsValidCoordinate(double.NaN, 0.0).Should().BeFalse();
        SearchCoordinationHub.IsValidCoordinate(0.0, double.NaN).Should().BeFalse();
    }

    [Fact]
    public void IsValidCoordinate_PositiveInfinity_ReturnsFalse()
    {
        SearchCoordinationHub.IsValidCoordinate(double.PositiveInfinity, 0.0).Should().BeFalse();
        SearchCoordinationHub.IsValidCoordinate(0.0, double.PositiveInfinity).Should().BeFalse();
    }

    [Fact]
    public void IsValidCoordinate_NegativeInfinity_ReturnsFalse()
    {
        SearchCoordinationHub.IsValidCoordinate(double.NegativeInfinity, 0.0).Should().BeFalse();
        SearchCoordinationHub.IsValidCoordinate(0.0, double.NegativeInfinity).Should().BeFalse();
    }

    // ── Out-of-range values ───────────────────────────────────────────────────

    [Theory]
    [InlineData(90.001, 0.0)]    // lat just above max
    [InlineData(-90.001, 0.0)]   // lat just below min
    [InlineData(0.0, 180.001)]   // lng just above max
    [InlineData(0.0, -180.001)]  // lng just below min
    [InlineData(999.0, 999.0)]   // wildly wrong
    public void IsValidCoordinate_OutOfRange_ReturnsFalse(double lat, double lng)
    {
        SearchCoordinationHub.IsValidCoordinate(lat, lng).Should().BeFalse();
    }

    // ── Boundary precision ────────────────────────────────────────────────────

    [Theory]
    [InlineData(90.0, 0.0)]
    [InlineData(-90.0, 0.0)]
    [InlineData(0.0, 180.0)]
    [InlineData(0.0, -180.0)]
    public void IsValidCoordinate_ExactBoundary_ReturnsTrue(double lat, double lng)
    {
        SearchCoordinationHub.IsValidCoordinate(lat, lng).Should().BeTrue();
    }
}
