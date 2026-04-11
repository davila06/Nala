using FluentAssertions;
using FluentValidation.TestHelper;
using PawTrack.Application.Sightings.Commands.ReportSighting;

namespace PawTrack.UnitTests.Sightings.Validators;

/// <summary>
/// Round-11 security: <see cref="ReportSightingCommandValidator"/> must reject
/// sightings whose <c>SightedAt</c> timestamp is before the platform launch date.
///
/// Without a lower-bound check, an attacker on the public sightings endpoint can submit
/// records timestamped in 1900, 2001, or any arbitrary date — polluting the platform's
/// statistics and recovery-rate calculations with phantom historical data.
/// </summary>
public sealed class ReportSightingTimestampValidatorTests
{
    private readonly ReportSightingCommandValidator _sut = new();

    // Earliest date PawTrack CR was operational — enforced as the lower bound
    // on SightedAt to prevent historically-impossible sighting records.
    private static readonly DateTimeOffset PlatformLaunchDate =
        new(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static ReportSightingCommand Valid(DateTimeOffset? sightedAt = null) =>
        new(
            PetId: Guid.NewGuid(),
            Lat: 9.9281,
            Lng: -84.0907,
            RawNote: null,
            PhotoStream: null,
            PhotoContentType: null,
            SightedAt: sightedAt ?? DateTimeOffset.UtcNow.AddHours(-1));

    // ── Lower-bound enforcement ───────────────────────────────────────────────

    /// <summary>
    /// SECURITY: Before the fix, this test FAILS because no lower-bound rule exists.
    /// After adding the rule, sightings before the platform launch date are rejected.
    /// </summary>
    [Fact]
    public void Validate_SightedAtBeforePlatformLaunch_FailsValidation()
    {
        var command = Valid(sightedAt: PlatformLaunchDate.AddSeconds(-1));
        var result  = _sut.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.SightedAt);
    }

    [Fact]
    public void Validate_SightedAtAncientDate_FailsValidation()
    {
        var command = Valid(sightedAt: new DateTimeOffset(1900, 1, 1, 0, 0, 0, TimeSpan.Zero));
        var result  = _sut.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.SightedAt);
    }

    [Fact]
    public void Validate_SightedAtEndOf2023_FailsValidation()
    {
        var command = Valid(sightedAt: new DateTimeOffset(2023, 12, 31, 23, 59, 59, TimeSpan.Zero));
        var result  = _sut.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.SightedAt);
    }

    // ── Valid timestamps ──────────────────────────────────────────────────────

    [Fact]
    public void Validate_SightedAtOnOrAfterPlatformLaunch_Passes()
    {
        var command = Valid(sightedAt: PlatformLaunchDate);
        var result  = _sut.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.SightedAt);
    }

    [Fact]
    public void Validate_SightedAtRecently_Passes()
    {
        var command = Valid(sightedAt: DateTimeOffset.UtcNow.AddHours(-2));
        var result  = _sut.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.SightedAt);
    }

    // ── Existing upper-bound still works ─────────────────────────────────────

    [Fact]
    public void Validate_SightedAtSixMinutesInFuture_FailsValidation()
    {
        var command = Valid(sightedAt: DateTimeOffset.UtcNow.AddMinutes(6));
        var result  = _sut.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.SightedAt);
    }

    [Fact]
    public void Validate_SightedAtNow_Passes()
    {
        var command = Valid(sightedAt: DateTimeOffset.UtcNow);
        var result  = _sut.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.SightedAt);
    }
}
