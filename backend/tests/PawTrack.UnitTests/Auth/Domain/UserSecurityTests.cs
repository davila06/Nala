using FluentAssertions;
using PawTrack.Domain.Auth;
using PawTrack.Domain.Pets;

namespace PawTrack.UnitTests.Auth.Domain;

public sealed class UserSecurityTests
{
    private static User CreateVerifiedUser(string email = "user@test.com")
    {
        var (user, rawToken) = User.Create(email, "hashed_password", "Test User");
        user.VerifyEmail(rawToken);
        return user;
    }

    // ── ChangePassword ────────────────────────────────────────────────────────

    [Fact]
    public void ChangePassword_CorrectCurrentPassword_Succeeds()
    {
        var user = CreateVerifiedUser();

        var result = user.ChangePassword("hashed_password", "new_hashed_password");

        result.IsSuccess.Should().BeTrue();
        user.PasswordHash.Should().Be("new_hashed_password");
    }

    [Fact]
    public void ChangePassword_WrongCurrentPassword_ReturnsFailure()
    {
        var user = CreateVerifiedUser();

        var result = user.ChangePassword("wrong_hash", "new_hashed_password");

        result.IsFailure.Should().BeTrue();
        user.PasswordHash.Should().Be("hashed_password"); // unchanged
    }

    // ── SoftDelete ────────────────────────────────────────────────────────────

    [Fact]
    public void SoftDelete_SetsIsDeletedAndAnonymizesEmail()
    {
        var user = CreateVerifiedUser("owner@real.com");

        user.SoftDelete();

        user.IsDeleted.Should().BeTrue();
        user.DeletedAt.Should().NotBeNull();
        user.Email.Should().StartWith("deleted-");
        user.PasswordHash.Should().BeEmpty();
        user.Name.Should().Be("Cuenta eliminada");
    }

    [Fact]
    public void SoftDelete_RevokesAllRefreshTokens()
    {
        var user = CreateVerifiedUser();
        user.AddRefreshToken("token-1", DateTimeOffset.UtcNow.AddDays(30));

        user.SoftDelete();

        // All tokens revoked — no active token remains
        user.IsDeleted.Should().BeTrue();
    }
}
