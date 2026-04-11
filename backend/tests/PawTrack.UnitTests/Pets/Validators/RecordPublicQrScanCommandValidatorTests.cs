using FluentAssertions;
using FluentValidation;
using FluentValidation.TestHelper;
using PawTrack.Application.Pets.Commands.RecordPublicQrScan;

namespace PawTrack.UnitTests.Pets.Validators;

/// <summary>
/// Round-12 security: <see cref="RecordPublicQrScanCommandValidator"/> must reject
/// out-of-range GPS coordinates supplied via the public QR-scan endpoint
/// (<c>GET /api/public/pets/{id}?scanLat=...&amp;scanLng=...</c>).
///
/// Without this validator, any client visiting a public pet profile page can inject
/// coordinates like <c>lat=9999</c> that:
/// 1. Pollute the proximity-to-owner home test (<c>IsNearOwnerHomeAsync</c>), potentially
///    triggering false "did you find your pet?" notifications.
/// 2. Get stored in the <c>QrScanEvent</c> audit log as geographically impossible data.
/// </summary>
public sealed class RecordPublicQrScanCommandValidatorTests
{
    private readonly RecordPublicQrScanCommandValidator _sut = new();

    private static RecordPublicQrScanCommand Valid(
        double? scanLat = null, double? scanLng = null) =>
        new(
            PetId:          Guid.NewGuid(),
            ScannedByUserId: null,
            IpAddress:      "10.0.0.1",
            UserAgent:      "Mozilla/5.0",
            CountryCode:    "CR",
            CityName:       "San José",
            ScannedAt:      DateTimeOffset.UtcNow,
            ScanLat:        scanLat,
            ScanLng:        scanLng);

    // ── PetId required ───────────────────────────────────────────────────────

    [Fact]
    public void Validate_EmptyPetId_FailsValidation()
    {
        var cmd = Valid() with { PetId = Guid.Empty };
        _sut.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.PetId);
    }

    // ── GPS: valid coords pass ────────────────────────────────────────────────

    [Fact]
    public void Validate_NullCoordinates_Passes()
    {
        // No GPS sent — common case for desktop browsers
        _sut.TestValidate(Valid()).ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(9.9281, -84.0907)]   // San José CR
    [InlineData(90.0, 180.0)]        // boundary valid
    [InlineData(-90.0, -180.0)]      // boundary valid
    [InlineData(0.0, 0.0)]           // equator/prime meridian
    public void Validate_ValidCoordinates_Passes(double lat, double lng)
    {
        _sut.TestValidate(Valid(lat, lng)).ShouldNotHaveAnyValidationErrors();
    }

    // ── GPS: out-of-range coords must fail ────────────────────────────────────

    /// <summary>
    /// SECURITY: Before the validator is added, these tests FAIL (no validator exists).
    /// After adding it, out-of-range coordinates are rejected before reaching the handler.
    /// </summary>
    [Theory]
    [InlineData(91.0, 0.0)]       // lat > 90
    [InlineData(-91.0, 0.0)]      // lat < -90
    [InlineData(9999.0, 0.0)]     // wildly out of range
    [InlineData(-9999.0, 0.0)]
    public void Validate_LatOutOfRange_FailsValidation(double lat, double lng)
    {
        var result = _sut.TestValidate(Valid(lat, lng));
        result.ShouldHaveValidationErrorFor(x => x.ScanLat);
    }

    [Theory]
    [InlineData(0.0, 181.0)]     // lng > 180
    [InlineData(0.0, -181.0)]    // lng < -180
    [InlineData(0.0, 9999.0)]
    public void Validate_LngOutOfRange_FailsValidation(double lat, double lng)
    {
        var result = _sut.TestValidate(Valid(lat, lng));
        result.ShouldHaveValidationErrorFor(x => x.ScanLng);
    }

    // ── Only one coord supplied should fail ───────────────────────────────────

    [Fact]
    public void Validate_OnlyLatSupplied_FailsValidation()
    {
        // ScanLat provided but ScanLng is null — coordinates must be paired
        var result = _sut.TestValidate(Valid(scanLat: 9.9281, scanLng: null));
        result.ShouldHaveValidationErrorFor(x => x.ScanLng);
    }

    [Fact]
    public void Validate_OnlyLngSupplied_FailsValidation()
    {
        var result = _sut.TestValidate(Valid(scanLat: null, scanLng: -84.0907));
        result.ShouldHaveValidationErrorFor(x => x.ScanLat);
    }

    // ── UserAgent and CityName length ─────────────────────────────────────────

    [Fact]
    public void Validate_UserAgentTooLong_FailsValidation()
    {
        var cmd = Valid() with { UserAgent = new string('A', 513) }; // > 512
        _sut.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.UserAgent);
    }

    [Fact]
    public void Validate_CityNameTooLong_FailsValidation()
    {
        var cmd = Valid() with { CityName = new string('X', 101) }; // > 100
        _sut.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.CityName);
    }
}
