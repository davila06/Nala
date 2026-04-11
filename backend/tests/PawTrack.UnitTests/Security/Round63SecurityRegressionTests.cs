using FluentAssertions;
using FluentValidation;
using PawTrack.Application.Auth.Commands.Logout;

namespace PawTrack.UnitTests.Security;

/// <summary>
/// Round-63 security regression tests.
///
/// Gap: <c>LogoutCommand</c> has no FluentValidation validator.
/// The command carries two required fields:
///
///   <code>
///   public sealed record LogoutCommand(
///       Guid UserId,
///       string RefreshToken,
///       string? AccessTokenJti,        // optional — null if caller doesn't pass it
///       DateTimeOffset? AccessTokenExpiresAt)
///   </code>
///
/// Without a validator:
///   - <c>Guid.Empty</c> for <c>UserId</c> reaches the handler which revokes refresh
///     tokens for user 00000000-… — harmless if such a user doesn't exist, but it
///     wastes a DB round-trip and leaves the handler's graceful-failure path untested.
///   - An empty <c>RefreshToken</c> triggers <c>ComputeHash("")</c> and a DB query
///     for the hash of the empty string — the same timing-analysis issue as R62.
///
/// Fix:
///   Create <c>LogoutCommandValidator.cs</c> with <c>NotEmpty()</c> on
///   <c>UserId</c> and <c>RefreshToken</c>.  <c>AccessTokenJti</c> and
///   <c>AccessTokenExpiresAt</c> are intentionally optional (nullable) and
///   must NOT be required by the validator.
/// </summary>
public sealed class Round63SecurityRegressionTests
{
    // ── Validator existence ───────────────────────────────────────────────────

    [Fact]
    public void LogoutCommandValidator_MustExist()
    {
        FindValidatorType().Should().NotBeNull(
            "the MediatR ValidationBehavior pipeline requires an " +
            "IValidator<LogoutCommand> to guard UserId and RefreshToken");
    }

    // ── Guid.Empty UserId rejected ────────────────────────────────────────────

    [Fact]
    public void Validator_RejectsEmptyUserId()
    {
        var result = CreateValidator().Validate(
            new LogoutCommand(Guid.Empty, "valid-token", null, null));

        result.IsValid.Should().BeFalse(
            "Guid.Empty UserId must be rejected before the handler attempts " +
            "to revoke refresh tokens for user 00000000-…");

        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(LogoutCommand.UserId));
    }

    // ── Empty / whitespace RefreshToken rejected ──────────────────────────────

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validator_RejectsEmptyOrWhitespaceRefreshToken(string token)
    {
        var result = CreateValidator().Validate(
            new LogoutCommand(Guid.NewGuid(), token, null, null));

        result.IsValid.Should().BeFalse(
            "an empty refresh token must be rejected to avoid hashing the empty string and " +
            "querying the DB unnecessarily");

        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(LogoutCommand.RefreshToken));
    }

    // ── Null optional fields are accepted ─────────────────────────────────────

    [Fact]
    public void Validator_AcceptsNullOptionalFields()
    {
        // AccessTokenJti and AccessTokenExpiresAt are intentionally optional
        var result = CreateValidator().Validate(
            new LogoutCommand(Guid.NewGuid(), "valid-token", null, null));

        result.Errors.Should()
            .NotContain(e =>
                e.PropertyName == nameof(LogoutCommand.AccessTokenJti) ||
                e.PropertyName == nameof(LogoutCommand.AccessTokenExpiresAt),
            "the optional nullable fields must never be required");
    }

    // ── Well-formed command passes ────────────────────────────────────────────

    [Fact]
    public void Validator_AcceptsWellFormedCommand()
    {
        var result = CreateValidator().Validate(
            new LogoutCommand(Guid.NewGuid(), "valid-refresh-token",
                "jti-claim-value", DateTimeOffset.UtcNow.AddMinutes(14)));

        result.IsValid.Should().BeTrue(
            "a fully-populated command with valid GUIDs and token must pass validation");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static Type? FindValidatorType() =>
        typeof(LogoutCommand).Assembly.GetTypes()
            .FirstOrDefault(t =>
                !t.IsAbstract && t.IsClass &&
                t.GetInterfaces().Any(i =>
                    i.IsGenericType &&
                    i.GetGenericTypeDefinition() == typeof(IValidator<>) &&
                    i.GetGenericArguments()[0] == typeof(LogoutCommand)));

    private static IValidator<LogoutCommand> CreateValidator()
    {
        var type = FindValidatorType()
            ?? throw new InvalidOperationException(
                "No IValidator<LogoutCommand> found.");
        return (IValidator<LogoutCommand>)Activator.CreateInstance(type)!;
    }
}
