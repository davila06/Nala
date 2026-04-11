using FluentAssertions;
using FluentValidation.TestHelper;
using PawTrack.Application.Chat.Commands.SendChatMessage;

namespace PawTrack.UnitTests.Chat;

/// <summary>
/// Round-8 security: SendChatMessageCommand must have a FluentValidation validator
/// in the MediatR pipeline so that validation is enforced declaratively, not only
/// inside the handler. This prevents body-length bypass if the handler is ever
/// refactored to skip the manual guards.
/// </summary>
public sealed class SendChatMessageCommandValidatorTests
{
    private readonly SendChatMessageCommandValidator _sut = new();

    private static SendChatMessageCommand Valid(string body = "Hola, ¿cómo estás?") =>
        new(Guid.NewGuid(), Guid.NewGuid(), body);

    // ── Empty / whitespace ────────────────────────────────────────────────────

    [Fact]
    public void Validate_EmptyBody_FailsValidation()
    {
        var result = _sut.TestValidate(Valid(string.Empty));
        result.ShouldHaveValidationErrorFor(x => x.Body);
    }

    [Fact]
    public void Validate_WhitespaceBody_FailsValidation()
    {
        var result = _sut.TestValidate(Valid("   "));
        result.ShouldHaveValidationErrorFor(x => x.Body);
    }

    // ── Length bounds ─────────────────────────────────────────────────────────

    [Fact]
    public void Validate_BodyAtExactMaxLength_Passes()
    {
        // 800 mirrors Guards.MaxBodyLength defined in SendChatMessageCommand.cs
        var body = new string('x', 800);
        var result = _sut.TestValidate(Valid(body));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_BodyExceedsMaxLength_FailsValidation()
    {
        var body = new string('x', 801);
        var result = _sut.TestValidate(Valid(body));
        result.ShouldHaveValidationErrorFor(x => x.Body);
    }

    // ── Normal cases ─────────────────────────────────────────────────────────

    [Fact]
    public void Validate_NormalMessage_Passes()
    {
        var result = _sut.TestValidate(Valid("Vi al perro en el parque."));
        result.ShouldNotHaveAnyValidationErrors();
    }
}
