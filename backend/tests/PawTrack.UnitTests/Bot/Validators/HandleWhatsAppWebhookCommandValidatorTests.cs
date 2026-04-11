using FluentAssertions;
using FluentValidation.TestHelper;
using PawTrack.Application.Bot.Commands.HandleWhatsAppWebhook;

namespace PawTrack.UnitTests.Bot.Validators;

public sealed class HandleWhatsAppWebhookCommandValidatorTests
{
    private readonly HandleWhatsAppWebhookCommandValidator _sut = new();

    private static HandleWhatsAppWebhookCommand ValidCommand(
        string waId = "50612345678",
        string messageId = "wamid.abc123",
        string messageType = "text",
        string? textBody = "Hola") =>
        new(waId, messageId, messageType, textBody, null, null, null);

    [Fact]
    public void ValidTextMessage_ShouldPass()
    {
        _sut.TestValidate(ValidCommand()).ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    public void WaId_Empty_ShouldFail(string waId)
    {
        _sut.TestValidate(ValidCommand(waId: waId))
            .ShouldHaveValidationErrorFor(x => x.WaId);
    }

    [Fact]
    public void WaId_TooLong_ShouldFail()
    {
        _sut.TestValidate(ValidCommand(waId: new string('5', 25)))
            .ShouldHaveValidationErrorFor(x => x.WaId);
    }

    [Theory]
    [InlineData("not-a-phone")]    // letters
    [InlineData("+506-1234-5678")] // has dashes
    public void WaId_InvalidFormat_ShouldFail(string waId)
    {
        _sut.TestValidate(ValidCommand(waId: waId))
            .ShouldHaveValidationErrorFor(x => x.WaId);
    }

    [Fact]
    public void MessageId_TooLong_ShouldFail()
    {
        _sut.TestValidate(ValidCommand(messageId: new string('w', 513)))
            .ShouldHaveValidationErrorFor(x => x.MessageId);
    }

    [Fact]
    public void TextBody_TooLong_ShouldFail()
    {
        var tooLong = new string('A', 4097); // > 4096
        _sut.TestValidate(ValidCommand(textBody: tooLong))
            .ShouldHaveValidationErrorFor(x => x.TextBody);
    }

    [Fact]
    public void TextBody_Null_ShouldPass()
    {
        // Non-text message types (e.g. location, image) may have null body
        _sut.TestValidate(ValidCommand(messageType: "location", textBody: null))
            .ShouldNotHaveAnyValidationErrors();
    }

    // ── Round-12 security: GPS range validation on location messages ──────────

    private static HandleWhatsAppWebhookCommand LocationCommand(
        double? lat, double? lng, string? address = null) =>
        new("50612345678", "wamid.loc1", "location", null, lat, lng, address);

    /// <summary>
    /// SECURITY: Before the fix, this test FAILS because no GPS bounds are checked.
    /// Out-of-range coordinate values (±999) must be rejected to prevent phantom
    /// data from being stored in the BotSession and then forwarded to LostPetEvent.
    /// </summary>
    [Theory]
    [InlineData(91.0, 0.0)]      // lat too high
    [InlineData(-91.0, 0.0)]     // lat too low
    [InlineData(0.0, 181.0)]     // lng too high
    [InlineData(0.0, -181.0)]    // lng too low
    [InlineData(999.0, 999.0)]   // both out of range
    public void LocationMessage_OutOfRangeCoordinates_ShouldFail(double lat, double lng)
    {
        var result = _sut.TestValidate(LocationCommand(lat, lng));
        // At least one coordinate field must have an error
        var hasError = result.Errors.Any(e =>
            e.PropertyName == nameof(HandleWhatsAppWebhookCommand.LocationLat) ||
            e.PropertyName == nameof(HandleWhatsAppWebhookCommand.LocationLng));
        hasError.Should().BeTrue(
            "out-of-range GPS coordinates on a location message must be rejected");
    }

    [Theory]
    [InlineData(9.9281, -84.0907)]   // San José CR
    [InlineData(90.0, 180.0)]        // boundary valid
    [InlineData(-90.0, -180.0)]      // boundary valid
    [InlineData(0.0, 0.0)]           // equator/meridian
    public void LocationMessage_ValidCoordinates_ShouldPass(double lat, double lng)
    {
        _sut.TestValidate(LocationCommand(lat, lng))
            .ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void LocationMessage_NullCoordinates_ShouldPass()
    {
        // Non-location messages have null coords — must not fail
        _sut.TestValidate(ValidCommand(messageType: "text", textBody: "hola"))
            .ShouldNotHaveAnyValidationErrors();
    }
}
