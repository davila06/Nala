using FluentAssertions;
using PawTrack.Domain.Auth;

namespace PawTrack.UnitTests.Auth.Domain;

public sealed class UserPasswordResetTests
{
    [Fact]
    public void IssuePasswordResetToken_ReturnsRawToken_AndStoresHash()
    {
        var (user, _) = User.Create("test@example.com", "hashed", "Test User");

        var rawToken = user.IssuePasswordResetToken();

        rawToken.Should().NotBeNullOrWhiteSpace();
        user.PasswordResetToken.Should().Be(User.ToHexHash(rawToken));
        user.PasswordResetTokenExpiry.Should().NotBeNull();
        user.PasswordResetTokenExpiry.Should().BeAfter(DateTimeOffset.UtcNow);
    }

    [Fact]
    public void ResetPassword_WithValidRawToken_UpdatesPasswordAndClearsToken()
    {
        var (user, verificationRawToken) = User.Create("test@example.com", "old_hash", "Test User");
        user.VerifyEmail(verificationRawToken);

        user.RecordFailedLogin();
        user.RecordFailedLogin();

        var resetRawToken = user.IssuePasswordResetToken();

        var updated = user.ResetPassword(resetRawToken, "new_hash");

        updated.Should().BeTrue();
        user.PasswordHash.Should().Be("new_hash");
        user.PasswordResetToken.Should().BeNull();
        user.PasswordResetTokenExpiry.Should().BeNull();
        user.FailedLoginAttempts.Should().Be(0);
        user.LockoutEnd.Should().BeNull();
    }

    [Fact]
    public void ResetPassword_WithInvalidToken_ReturnsFalse_AndDoesNotChangePassword()
    {
        var (user, _) = User.Create("test@example.com", "old_hash", "Test User");
        user.IssuePasswordResetToken();

        var updated = user.ResetPassword("not-the-token", "new_hash");

        updated.Should().BeFalse();
        user.PasswordHash.Should().Be("old_hash");
    }
}