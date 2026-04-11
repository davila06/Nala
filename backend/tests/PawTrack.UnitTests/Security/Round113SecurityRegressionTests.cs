using FluentAssertions;
using NSubstitute;
using PawTrack.Application.Auth.Queries.GetMyProfile;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Auth;

namespace PawTrack.UnitTests.Security;

/// <summary>
/// SEC-05 (R113) — <see cref="UserProfileDto"/> must not expose the raw <c>Role</c> string
/// in API responses.
///
/// Vulnerability (before fix): The endpoint returned strings like "Admin", "Ally", or "Clinic",
/// allowing unauthenticated observers to enumerate internal role identifiers that map the
/// authorisation surface of the system. Enumerating privilege levels is an OWASP API5
/// (Broken Function-Level Authorization) information-disclosure precursor.
///
/// Fix: Both <c>Auth.DTOs.UserProfileDto</c> and <c>Auth.Queries.GetMyProfile.UserProfileDto</c>
/// replace <c>string Role</c> with <c>bool IsAdmin</c>. The boolean exposes only the capability
/// the front-end legitimately needs (to show/hide admin UI) without surfacing the full
/// internal role taxonomy.
/// </summary>
public sealed class Round113SecurityRegressionTests
{
    // ── R113-A: Auth.DTOs.UserProfileDto — no Role property ─────────────────

    [Fact]
    public void R113_AuthDtoUserProfileDto_DoesNotExposeRoleString()
    {
        var prop = typeof(PawTrack.Application.Auth.DTOs.UserProfileDto).GetProperty("Role");

        prop.Should().BeNull(
            because: "exposing the raw Role string leaks internal authorisation surface area " +
                     "and assists privilege-escalation reconnaissance (OWASP API5)");
    }

    [Fact]
    public void R113_AuthDtoUserProfileDto_AdminUser_IsAdminTrue()
    {
        var (user, rawToken) = User.Create("admin@example.com", "hashed", "Admin User");
        user.VerifyEmail(rawToken);
        user.PromoteToAdmin();

        var dto = PawTrack.Application.Auth.DTOs.UserProfileDto.FromDomain(user);

        dto.IsAdmin.Should().BeTrue(
            because: "Admin role user must be identified as admin in the profile DTO");
    }

    [Fact]
    public void R113_AuthDtoUserProfileDto_OwnerUser_IsAdminFalse()
    {
        var (user, rawToken) = User.Create("owner@example.com", "hashed", "Owner User");
        user.VerifyEmail(rawToken);

        var dto = PawTrack.Application.Auth.DTOs.UserProfileDto.FromDomain(user);

        dto.IsAdmin.Should().BeFalse(
            because: "Owner role must not be granted admin capability");
    }

    [Fact]
    public void R113_AuthDtoUserProfileDto_AllyUser_IsAdminFalse()
    {
        var (user, rawToken) = User.Create("ally@example.com", "hashed", "Ally User");
        user.VerifyEmail(rawToken);
        user.PromoteToAlly();

        var dto = PawTrack.Application.Auth.DTOs.UserProfileDto.FromDomain(user);

        dto.IsAdmin.Should().BeFalse(
            because: "Ally role must not be granted admin capability");
    }

    // ── R113-B: GetMyProfile query DTO — no Role property ───────────────────

    [Fact]
    public void R113_GetMyProfileDto_DoesNotExposeRoleString()
    {
        var prop = typeof(PawTrack.Application.Auth.Queries.GetMyProfile.UserProfileDto)
            .GetProperty("Role");

        prop.Should().BeNull(
            because: "the /api/auth/me endpoint must not expose the raw Role string");
    }

    [Fact]
    public async Task R113_GetMyProfileHandler_AdminUser_ReturnsIsAdminTrue()
    {
        // Arrange
        var userRepo = Substitute.For<IUserRepository>();
        var userId   = Guid.NewGuid();
        var (user, rawToken) = User.Create("admin@example.com", "hashed", "Admin");
        user.VerifyEmail(rawToken);
        user.PromoteToAdmin();

        userRepo.GetByIdAsync(userId, Arg.Any<CancellationToken>()).Returns(user);

        var handler = new GetMyProfileQueryHandler(userRepo);

        // Act
        var result = await handler.Handle(new GetMyProfileQuery(userId), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.IsAdmin.Should().BeTrue();
    }

    [Fact]
    public async Task R113_GetMyProfileHandler_OwnerUser_ReturnsIsAdminFalse()
    {
        // Arrange
        var userRepo = Substitute.For<IUserRepository>();
        var userId   = Guid.NewGuid();
        var (user, rawToken) = User.Create("owner@example.com", "hashed", "Owner");
        user.VerifyEmail(rawToken);

        userRepo.GetByIdAsync(userId, Arg.Any<CancellationToken>()).Returns(user);

        var handler = new GetMyProfileQueryHandler(userRepo);

        // Act
        var result = await handler.Handle(new GetMyProfileQuery(userId), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.IsAdmin.Should().BeFalse();
    }

    [Fact]
    public async Task R113_GetMyProfileHandler_UserNotFound_ReturnsFailure()
    {
        // Arrange
        var userRepo = Substitute.For<IUserRepository>();
        userRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
                .Returns((User?)null);

        var handler = new GetMyProfileQueryHandler(userRepo);

        // Act
        var result = await handler.Handle(new GetMyProfileQuery(Guid.NewGuid()), CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
    }
}
