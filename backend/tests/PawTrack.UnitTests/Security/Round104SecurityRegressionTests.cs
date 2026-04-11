using FluentValidation.TestHelper;
using PawTrack.Application.Sightings.Queries.GetPublicMapEvents;

namespace PawTrack.UnitTests.Security;

/// <summary>
/// R104 — Bbox coordinate range validation for GetPublicMapEventsQuery.
/// Latitude must be in [-90, 90], longitude in [-180, 180].
/// North must be greater than South; East must be greater than West.
/// </summary>
public sealed class Round104SecurityRegressionTests
{
    private readonly GetPublicMapEventsQueryValidator _validator = new();

    private static GetPublicMapEventsQuery Valid() =>
        new(North: 10, South: -10, East: 10, West: -10);

    [Fact]
    public void R104_ValidBbox_Passes()
        => _validator.TestValidate(Valid())
                     .ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void R104_NorthOutOfRange_Fails()
        => _validator.TestValidate(Valid() with { North = 91 })
                     .ShouldHaveValidationErrorFor(x => x.North);

    [Fact]
    public void R104_SouthOutOfRange_Fails()
        => _validator.TestValidate(Valid() with { South = -91 })
                     .ShouldHaveValidationErrorFor(x => x.South);

    [Fact]
    public void R104_EastOutOfRange_Fails()
        => _validator.TestValidate(Valid() with { East = 181 })
                     .ShouldHaveValidationErrorFor(x => x.East);

    [Fact]
    public void R104_WestOutOfRange_Fails()
        => _validator.TestValidate(Valid() with { West = -181 })
                     .ShouldHaveValidationErrorFor(x => x.West);

    [Fact]
    public void R104_NorthLessThanSouth_Fails()
        => _validator.TestValidate(Valid() with { North = -20, South = 20 })
                     .ShouldHaveValidationErrorFor(x => x.North);

    [Fact]
    public void R104_EastLessThanWest_Fails()
        => _validator.TestValidate(Valid() with { East = -20, West = 20 })
                     .ShouldHaveValidationErrorFor(x => x.East);
}
