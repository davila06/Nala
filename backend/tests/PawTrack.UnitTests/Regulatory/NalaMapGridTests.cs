using FluentAssertions;
using PawTrack.Application.Regulatory.Services;

namespace PawTrack.UnitTests.Regulatory;

public sealed class NalaMapGridTests
{
    [Fact]
    public void Aggregate_UsesTruncatedGridAndSuppressesSmallCells()
    {
        var points = new[]
        {
            new MapPoint(9.934, -84.081, "Lost", "San José"),
            new MapPoint(9.936, -84.083, "Lost", "San José"),
            new MapPoint(9.5, -84.2, "Welfare", "Heredia"),
        };

        var result = NalaMapGridService.Aggregate(points, south: 9, north: 10, west: -85, east: -83, threshold: 2);

        result.Should().ContainSingle(cell => cell.IsSuppressed && cell.Canton == "Heredia");
        result.Should().ContainSingle(cell => !cell.IsSuppressed && cell.Canton == "San José");
        result[1].Latitude.Should().NotBe(9.5);
    }
}
