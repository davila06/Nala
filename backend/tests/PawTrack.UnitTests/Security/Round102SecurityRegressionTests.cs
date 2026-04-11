using FluentValidation.TestHelper;
using PawTrack.Application.Auth.Commands.Logout;
using PawTrack.Application.Auth.Commands.RefreshToken;

namespace PawTrack.UnitTests.Security;

/// <summary>
/// R102 — Missing MaximumLength on JWT token strings in existing validators.
/// A 10 MB token string must be rejected before reaching the handler.
/// </summary>
public sealed class Round102SecurityRegressionTests
{
    private readonly RefreshTokenCommandValidator _refreshValidator = new();
    private readonly LogoutCommandValidator _logoutValidator = new();

    private static string LongString(int length) => new('x', length);

    // ── RefreshTokenCommand ──────────────────────────────────────────────────

    [Fact]
    public void R102_RefreshToken_TokenExceedsMaxLength_Fails()
    {
        var cmd = new RefreshTokenCommand(LongString(2049));
        _refreshValidator.TestValidate(cmd)
            .ShouldHaveValidationErrorFor(x => x.Token);
    }

    [Fact]
    public void R102_RefreshToken_TokenAtMaxLength_Passes()
    {
        var cmd = new RefreshTokenCommand(LongString(2048));
        _refreshValidator.TestValidate(cmd)
            .ShouldNotHaveAnyValidationErrors();
    }

    // ── LogoutCommand ────────────────────────────────────────────────────────

    [Fact]
    public void R102_Logout_RefreshTokenExceedsMaxLength_Fails()
    {
        var cmd = new LogoutCommand(Guid.NewGuid(), LongString(2049), null, null);
        _logoutValidator.TestValidate(cmd)
            .ShouldHaveValidationErrorFor(x => x.RefreshToken);
    }

    [Fact]
    public void R102_Logout_AccessTokenJtiExceedsMaxLength_Fails()
    {
        // JTI is a UUID string (max 36 chars); anything longer is invalid
        var cmd = new LogoutCommand(Guid.NewGuid(), "valid-token", LongString(37), null);
        _logoutValidator.TestValidate(cmd)
            .ShouldHaveValidationErrorFor(x => x.AccessTokenJti);
    }

    [Fact]
    public void R102_Logout_ValidTokens_Passes()
    {
        var cmd = new LogoutCommand(
            Guid.NewGuid(),
            LongString(2048),
            Guid.NewGuid().ToString(),
            DateTimeOffset.UtcNow.AddMinutes(5));
        _logoutValidator.TestValidate(cmd)
            .ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void R102_Logout_NullOptionalFields_Passes()
    {
        var cmd = new LogoutCommand(Guid.NewGuid(), "valid-token", null, null);
        _logoutValidator.TestValidate(cmd)
            .ShouldNotHaveAnyValidationErrors();
    }
}
