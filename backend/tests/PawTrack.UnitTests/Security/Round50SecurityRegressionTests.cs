using FluentAssertions;
using PawTrack.Application.LostPets.Queries.GetRecoveryRates;

namespace PawTrack.UnitTests.Security;

/// <summary>
/// Round-50 security regression tests.
///
/// Gap: <c>GET /api/public/stats/recovery-rates?species=…&amp;breed=…&amp;canton=…</c>
/// has no <c>FluentValidation</c> validator.  The <c>species</c>, <c>breed</c>,
/// and <c>canton</c> parameters are unbounded strings that flow directly into
/// the repository query and ultimately into SQL (via EF Core parameterization)
/// and Application Insights telemetry.
///
/// Consequences:
/// <list type="number">
///   <item>
///     <b>Log injection / telemetry flooding</b> — arbitrarily long query
///     strings are recorded verbatim in Application Insights traces.
///     A caller that sends a 1 MB <c>breed</c> value pollutes telemetry,
///     inflates storage costs, and may exceed log entry size limits, causing
///     trace loss.
///   </item>
///   <item>
///     <b>Memory pressure</b> — very long strings are allocated as <c>string</c>
///     objects in the managed heap before reaching EF Core.
///   </item>
/// </list>
///
/// Fix:
///   Create <c>GetRecoveryRatesQueryValidator</c> with <c>MaximumLength</c> rules
///   on all three nullable filter parameters.
/// </summary>
public sealed class Round50SecurityRegressionTests
{
    private readonly GetRecoveryRatesQueryValidator _sut = new();

    [Theory]
    [InlineData(101, null,  null)]  // breed too long
    [InlineData(null, 101,  null)]  // species too long (using index as length marker)
    [InlineData(null, null, 101)]   // canton too long
    public void Validator_RejectsFilterParams_ExceedingMaximumLength(
        int? breedLen, int? speciesLen, int? cantonLen)
    {
        var breed   = breedLen.HasValue   ? new string('b', breedLen.Value)   : null;
        var species = speciesLen.HasValue ? new string('s', speciesLen.Value) : null;
        var canton  = cantonLen.HasValue  ? new string('c', cantonLen.Value)  : null;

        var result = _sut.Validate(new GetRecoveryRatesQuery(species, breed, canton));

        result.IsValid.Should().BeFalse(
            "filter parameters must be bounded to prevent telemetry flooding and memory pressure");
    }

    [Fact]
    public void Validator_AcceptsNullFilterParams()
    {
        // All params are optional (nullable); null must pass
        var result = _sut.Validate(new GetRecoveryRatesQuery(null, null, null));

        result.IsValid.Should().BeTrue(
            "null filter parameters are the default case — all pets, all breeds, all cantons");
    }

    [Fact]
    public void Validator_AcceptsValidFilterParams()
    {
        var result = _sut.Validate(
            new GetRecoveryRatesQuery("Dog", "Golden Retriever", "San José"));

        result.IsValid.Should().BeTrue(
            "typical real-world filter values must not be rejected");
    }

    [Fact]
    public void Validator_RejectsBreed_ExactlyAtBoundary()
    {
        // 101 chars - 1 over the limit
        var breed = new string('x', 101);
        var result = _sut.Validate(new GetRecoveryRatesQuery(null, breed, null));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(GetRecoveryRatesQuery.Breed));
    }
}
