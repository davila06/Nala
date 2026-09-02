using FluentAssertions;
using PawTrack.Domain.Collars;

namespace PawTrack.UnitTests.Collars;

public sealed class GeoPolygonTests
{
    // A simple square around San José, CR: (9.9,-84.1) .. (10.0,-84.0)
    private static readonly List<(double Lat, double Lng)> Square =
    [
        (9.9, -84.1),
        (9.9, -84.0),
        (10.0, -84.0),
        (10.0, -84.1),
    ];

    [Fact]
    public void Contains_PointInsideSquare_ReturnsTrue()
    {
        GeoPolygon.Contains(Square, 9.95, -84.05).Should().BeTrue();
    }

    [Fact]
    public void Contains_PointOutsideSquare_ReturnsFalse()
    {
        GeoPolygon.Contains(Square, 8.0, -83.0).Should().BeFalse();
    }

    [Fact]
    public void Contains_FewerThanThreePoints_ReturnsFalse()
    {
        GeoPolygon.Contains([(9.9, -84.1), (10.0, -84.0)], 9.95, -84.05).Should().BeFalse();
    }
}
