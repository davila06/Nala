using FluentValidation.TestHelper;
using PawTrack.Application.Notifications.Commands.RegisterPushSubscription;

namespace PawTrack.UnitTests.Notifications.Validators;

public sealed class RegisterPushSubscriptionCommandValidatorTests
{
    private readonly RegisterPushSubscriptionCommandValidator _sut = new();

    private static RegisterPushSubscriptionCommand ValidCommand(
        string endpoint = "https://fcm.googleapis.com/fcm/send/abc123",
        string keysJson = """{"auth":"abc","p256dh":"xyz"}""") =>
        new(Guid.NewGuid(), endpoint, keysJson);

    [Fact]
    public void ValidCommand_ShouldPassValidation()
    {
        _sut.TestValidate(ValidCommand()).ShouldNotHaveAnyValidationErrors();
    }

    // ── Endpoint ──────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("")]
    public void Endpoint_Empty_ShouldFail(string endpoint)
    {
        _sut.TestValidate(ValidCommand(endpoint: endpoint))
            .ShouldHaveValidationErrorFor(x => x.Endpoint);
    }

    [Fact]
    public void Endpoint_TooLong_ShouldFail()
    {
        var tooLong = "https://" + new string('a', 2050);
        _sut.TestValidate(ValidCommand(endpoint: tooLong))
            .ShouldHaveValidationErrorFor(x => x.Endpoint);
    }

    [Theory]
    [InlineData("http://fcm.googleapis.com/send")]   // http not https
    [InlineData("ftp://example.com/push")]
    [InlineData("not-a-url-at-all")]
    public void Endpoint_NotHttps_ShouldFail(string endpoint)
    {
        _sut.TestValidate(ValidCommand(endpoint: endpoint))
            .ShouldHaveValidationErrorFor(x => x.Endpoint);
    }

    // ── KeysJson ──────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("")]
    public void KeysJson_Empty_ShouldFail(string keysJson)
    {
        _sut.TestValidate(ValidCommand(keysJson: keysJson))
            .ShouldHaveValidationErrorFor(x => x.KeysJson);
    }

    [Fact]
    public void KeysJson_TooLong_ShouldFail()
    {
        var tooLong = new string('k', 1025);
        _sut.TestValidate(ValidCommand(keysJson: tooLong))
            .ShouldHaveValidationErrorFor(x => x.KeysJson);
    }
}
