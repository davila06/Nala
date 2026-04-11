using FluentAssertions;
using NSubstitute;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Fosters.Queries.GetFosterSuggestions;
using PawTrack.Domain.Pets;
using PawTrack.Domain.Sightings;

namespace PawTrack.UnitTests.Security;

/// <summary>
/// Round-21 security regression tests.
///
/// Gap: <c>FosterSuggestionDto.DistanceMetres</c> was returned as a precise
/// floating-point value (e.g., 247.3219 m). The public
/// <c>GET /api/found-pets/active</c> endpoint exposes each open found-pet
/// report's exact <c>FoundLat / FoundLng</c> along with its <c>Id</c>.
/// Any authenticated user can therefore:
///   1. Collect multiple found-pet report IDs + their GPS coordinates from
///      the public endpoint.
///   2. Call <c>GET /api/fosters/suggestions/from-found-report/{id}</c> for
///      each report (requires only a valid account).
///   3. Use the precise distance values from 3+ known GPS reference points to
///      triangulate each foster volunteer's home address to within metres —
///      a direct stalking / deanonymisation vector.
///
/// Fix: <c>GetFosterSuggestionsQueryHandler.MapToDto</c> rounds
/// <c>DistanceMetres</c> to the nearest 100 m before placing it in the DTO.
/// This limits triangulation accuracy to ±100 m (roughly a city block),
/// making address-level deanonymisation impractical.
/// </summary>
public sealed class Round21SecurityRegressionTests
{
    private readonly IFosterVolunteerRepository _fosterRepo =
        Substitute.For<IFosterVolunteerRepository>();

    private readonly IFoundPetRepository _foundPetRepo =
        Substitute.For<IFoundPetRepository>();

    private GetFosterSuggestionsQueryHandler CreateSut() =>
        new(_fosterRepo, _foundPetRepo);

    // ── Helpers ───────────────────────────────────────────────────────────────

    private FoundPetReport SeedReport()
    {
        var report = FoundPetReport.Create(
            PetSpecies.Dog, null, "Café", null,
            9.9300, -84.0800,
            "Reporter", "8888-0000", null);

        _foundPetRepo
            .GetByIdAsync(report.Id, Arg.Any<CancellationToken>())
            .Returns(report);

        return report;
    }

    private void SeedVolunteer(double rawDistanceMetres)
    {
        _fosterRepo
            .GetNearbyAvailableAsync(
                Arg.Any<double>(), Arg.Any<double>(),
                Arg.Any<PetSpecies>(), Arg.Any<int>(),
                Arg.Any<CancellationToken>())
            .Returns(new List<FosterVolunteerSuggestion>
            {
                new(Guid.NewGuid(), "VolunteerX", rawDistanceMetres, true, null, 5),
            });
    }

    // ── Test 1: sub-100-metre fractional distance rounds down ─────────────────

    /// <summary>
    /// A raw distance of 247 m must become 200 m in the DTO.
    /// Returning the precise value would let callers subtract reference-point
    /// GPS coordinates and triangulate the volunteer's front door.
    /// </summary>
    [Fact]
    public async Task FosterSuggestions_DistanceMetres_IsRoundedToNearest100()
    {
        var report = SeedReport();
        SeedVolunteer(247.0);

        var result = await CreateSut().Handle(
            new GetFosterSuggestionsQuery(report.Id, 3), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Single().DistanceMetres.Should().Be(200.0,
            "precise sub-100-metre distances expose volunteer home locations " +
            "via GPS triangulation; 247 m must round to 200 m");
    }

    // ── Test 2: super-1-km distance also rounds ────────────────────────────────

    /// <summary>
    /// A raw distance of 1 567 m must become 1 600 m in the DTO.
    /// </summary>
    [Fact]
    public async Task FosterSuggestions_OverKmDistance_IsRoundedToNearest100()
    {
        var report = SeedReport();
        SeedVolunteer(1567.0);

        var result = await CreateSut().Handle(
            new GetFosterSuggestionsQuery(report.Id, 3), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Single().DistanceMetres.Should().Be(1600.0,
            "1567 m rounds to the nearest 100 m → 1600 m");
    }

    // ── Test 3: DistanceLabel derives from rounded value ───────────────────────

    /// <summary>
    /// <c>DistanceLabel</c> must reflect the rounded distance (200 m), not the
    /// raw precise value (247 m), so front-end display cannot be used to
    /// back-calculate the exact distance.
    /// </summary>
    [Fact]
    public async Task FosterSuggestions_DistanceLabel_MatchesRoundedDistance()
    {
        var report = SeedReport();
        SeedVolunteer(247.0);

        var result = await CreateSut().Handle(
            new GetFosterSuggestionsQuery(report.Id, 3), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Single().DistanceLabel.Should().Be("200 m",
            "DistanceLabel must be derived from the rounded 100-m value (200 m), " +
            "not the raw precise value (247 m)");
    }

    // ── Test 4: over-km label uses rounded km value ────────────────────────────

    /// <summary>
    /// For a rounded distance ≥ 1 000 m (e.g., 1 600 m), the label must use
    /// the km representation derived from the rounded value.
    /// </summary>
    [Fact]
    public async Task FosterSuggestions_DistanceLabelKm_MatchesRoundedDistance()
    {
        var report = SeedReport();
        SeedVolunteer(1567.0);

        var result = await CreateSut().Handle(
            new GetFosterSuggestionsQuery(report.Id, 3), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Single().DistanceLabel.Should().Be("1.6 km",
            "DistanceLabel km form must derive from the rounded value 1600 m → 1.6 km");
    }

    // ── Test 5: parametrised multi-value coverage ──────────────────────────────

    /// <summary>
    /// Spot-checks several raw distances to verify the nearest-100-m rounding
    /// contract across the full range of plausible volunteer distances.
    /// </summary>
    [Theory]
    [InlineData(1.0,    0.0)]      // 1 m    → 0 m   (rounds to zero — trivially safe)
    [InlineData(247.0,  200.0)]    // 247 m  → 200 m (rounds down)
    [InlineData(1567.0, 1600.0)]   // 1567 m → 1600 m (rounds up)
    [InlineData(1999.9, 2000.0)]   // 1999.9 m → 2000 m
    [InlineData(2500.0, 2500.0)]   // exact multiple — unchanged
    public async Task FosterSuggestions_DistanceMetres_RoundsToNearest100_Parametrised(
        double rawMetres, double expectedRounded)
    {
        var report = SeedReport();
        SeedVolunteer(rawMetres);

        var result = await CreateSut().Handle(
            new GetFosterSuggestionsQuery(report.Id, 3), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Single().DistanceMetres.Should().Be(expectedRounded,
            $"raw {rawMetres} m must round to nearest 100 m → {expectedRounded} m");
    }
}
