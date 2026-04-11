using FluentAssertions;
using FluentValidation;
using PawTrack.Application.Auth.Commands.RefreshToken;

namespace PawTrack.UnitTests.Security;

/// <summary>
/// Round-62 security regression tests.
///
/// Gap: <c>RefreshTokenCommand</c> has no FluentValidation validator.
/// The single field <c>Token</c> is a string that must not be null or empty:
///
///   <code>
///   public sealed record RefreshTokenCommand(string Token)
///       : IRequest&lt;Result&lt;AuthTokenDto&gt;&gt;;
///   </code>
///
/// Without a validator, an empty or whitespace-only token bypasses the MediatR
/// pipeline and enters the handler, which calls <c>ComputeHash("")</c> and issues
/// a DB query for a hash of the empty string.  Although the handler returns a
/// graceful failure when the token is not found, the round-trip is unnecessary
/// and opens a timing-analysis channel: the response time for an empty-string
/// call should be identical to one with a well-formed fabricated token.
///
/// Additionally, a null <c>Token</c> (possible if the request body omits the field)
/// would throw <c>ArgumentNullException</c> inside <c>ComputeHash</c>, which
/// bypasses the handler's graceful-failure path and risks leaking stack details
/// before the <c>ExceptionHandlingMiddleware</c> catches it.
///
/// Fix:
///   Create <c>RefreshTokenCommandValidator.cs</c> with <c>.NotEmpty()</c> on Token.
/// </summary>
public sealed class Round62SecurityRegressionTests
{
    // ── Validator existence ───────────────────────────────────────────────────

    [Fact]
    public void RefreshTokenCommandValidator_MustExist()
    {
        var validatorType = FindValidatorType();

        validatorType.Should().NotBeNull(
            "the MediatR ValidationBehavior pipeline requires an " +
            "IValidator<RefreshTokenCommand> to guard the Token field " +
            "before the handler computes a hash and hits the DB");
    }

    // ── Empty / whitespace token rejected ─────────────────────────────────────

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validator_RejectsEmptyOrWhitespaceToken(string token)
    {
        var validator = CreateValidator();

        var result = validator.Validate(new RefreshTokenCommand(token));

        result.IsValid.Should().BeFalse(
            "an empty or whitespace-only refresh token must be rejected at the " +
            "validation layer to avoid an unnecessary DB round-trip and timing leak");

        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(RefreshTokenCommand.Token));
    }

    // ── Well-formed token passes ──────────────────────────────────────────────

    [Fact]
    public void Validator_AcceptsNonEmptyToken()
    {
        var validator = CreateValidator();

        var result = validator.Validate(
            new RefreshTokenCommand("eyJhbGciOiJIUzI1NiJ9.fake.token"));

        result.IsValid.Should().BeTrue(
            "a non-empty token string must pass validation so the handler can run");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static Type? FindValidatorType() =>
        typeof(RefreshTokenCommand).Assembly.GetTypes()
            .FirstOrDefault(t =>
                !t.IsAbstract && t.IsClass &&
                t.GetInterfaces().Any(i =>
                    i.IsGenericType &&
                    i.GetGenericTypeDefinition() == typeof(IValidator<>) &&
                    i.GetGenericArguments()[0] == typeof(RefreshTokenCommand)));

    private static IValidator<RefreshTokenCommand> CreateValidator()
    {
        var type = FindValidatorType()
            ?? throw new InvalidOperationException(
                "No IValidator<RefreshTokenCommand> found. Create the validator first.");
        return (IValidator<RefreshTokenCommand>)Activator.CreateInstance(type)!;
    }
}
