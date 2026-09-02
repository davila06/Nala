using System.Text.Json;
using FluentAssertions;
using PawTrack.Domain.Collars;

namespace PawTrack.UnitTests.Collars;

public sealed class CollarSafeZoneDomainTests
{
    private static readonly string SquarePolygonJson = JsonSerializer.Serialize(new[]
    {
        new { lat = 9.9, lng = -84.1 },
        new { lat = 9.9, lng = -84.0 },
        new { lat = 10.0, lng = -84.0 },
        new { lat = 10.0, lng = -84.1 },
    });

    [Fact]
    public void Create_ValidPolygon_Succeeds()
    {
        var zone = CollarSafeZone.Create(Guid.NewGuid(), "Casa", SquarePolygonJson);

        zone.Enabled.Should().BeTrue();
        zone.LastKnownInside.Should().BeNull();
    }

    [Fact]
    public void Create_FewerThanThreePoints_Throws()
    {
        var twoPoints = JsonSerializer.Serialize(new[]
        {
            new { lat = 9.9, lng = -84.1 },
            new { lat = 10.0, lng = -84.0 },
        });

        var act = () => CollarSafeZone.Create(Guid.NewGuid(), "Casa", twoPoints);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_InvalidJson_Throws()
    {
        var act = () => CollarSafeZone.Create(Guid.NewGuid(), "Casa", "not json");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Evaluate_FirstFix_EstablishesBaselineWithoutTransition()
    {
        var zone = CollarSafeZone.Create(Guid.NewGuid(), "Casa", SquarePolygonJson);

        var transition = zone.Evaluate(9.95, -84.05); // inside

        transition.Should().Be(SafeZoneTransition.NoChange);
        zone.LastKnownInside.Should().BeTrue();
    }

    [Fact]
    public void Evaluate_InsideThenOutside_ReturnsBreached()
    {
        var zone = CollarSafeZone.Create(Guid.NewGuid(), "Casa", SquarePolygonJson);
        zone.Evaluate(9.95, -84.05); // baseline: inside

        var transition = zone.Evaluate(8.0, -83.0); // now outside

        transition.Should().Be(SafeZoneTransition.Breached);
    }

    [Fact]
    public void Evaluate_OutsideThenInside_ReturnsReturned()
    {
        var zone = CollarSafeZone.Create(Guid.NewGuid(), "Casa", SquarePolygonJson);
        zone.Evaluate(8.0, -83.0); // baseline: outside

        var transition = zone.Evaluate(9.95, -84.05); // now inside

        transition.Should().Be(SafeZoneTransition.Returned);
    }

    [Fact]
    public void Evaluate_StaysInside_NoChange()
    {
        var zone = CollarSafeZone.Create(Guid.NewGuid(), "Casa", SquarePolygonJson);
        zone.Evaluate(9.95, -84.05);

        var transition = zone.Evaluate(9.96, -84.06);

        transition.Should().Be(SafeZoneTransition.NoChange);
    }

    [Fact]
    public void Update_ChangesPolygonAndResetsBaseline()
    {
        var zone = CollarSafeZone.Create(Guid.NewGuid(), "Casa", SquarePolygonJson);
        zone.Evaluate(9.95, -84.05);

        zone.Update("Nueva zona", SquarePolygonJson, enabled: false);

        zone.Name.Should().Be("Nueva zona");
        zone.Enabled.Should().BeFalse();
        zone.LastKnownInside.Should().BeNull();
    }
}
