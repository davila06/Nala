using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using PawTrack.Application.Auth.Commands.RefreshToken;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Auth;

namespace PawTrack.UnitTests.Auth.Handlers;

public sealed class RefreshTokenCommandHandlerTests
{
    private readonly IRefreshTokenRepository _tokenRepo = Substitute.For<IRefreshTokenRepository>();
    private readonly IUserRepository _userRepo = Substitute.For<IUserRepository>();
    private readonly IJwtTokenService _jwtService = Substitute.For<IJwtTokenService>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly ILogger<RefreshTokenCommandHandler> _logger = Substitute.For<ILogger<RefreshTokenCommandHandler>>();

    private readonly RefreshTokenCommandHandler _sut;

    public RefreshTokenCommandHandlerTests()
    {
        _sut = new RefreshTokenCommandHandler(_tokenRepo, _userRepo, _jwtService, _uow, _logger);
    }

    // ── Helper ────────────────────────────────────────────────────────────────

    private static User CreateVerifiedUser(string email = "user@example.com")
    {
        var (user, rawToken) = User.Create(email, "hashed", "Test User");
        user.VerifyEmail(rawToken);
        return user;
    }

    private static string ComputeHash(string token)
    {
        var bytes = System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    // ── Happy path ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_ValidToken_RotatesAndReturnsNewTokens()
    {
        const string rawToken = "valid_raw_token";
        var tokenHash = ComputeHash(rawToken);

        var user = CreateVerifiedUser();
        var activeToken = user.AddRefreshToken(tokenHash, DateTimeOffset.UtcNow.AddDays(30));

        _tokenRepo.GetByHashAsync(tokenHash, Arg.Any<CancellationToken>()).Returns(activeToken);
        _userRepo.GetByIdAsync(activeToken.UserId, Arg.Any<CancellationToken>()).Returns(user);
        _jwtService.GenerateRefreshToken().Returns(("new_raw", "new_hash"));
        _jwtService.GenerateAccessToken(user.Id, user.Email, user.Name, user.Role).Returns("new_access");
        _jwtService.AccessTokenExpirySeconds.Returns(900);

        var result = await _sut.Handle(new RefreshTokenCommand(rawToken), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.AccessToken.Should().Be("new_access");
        result.Value.RefreshToken.Should().Be("new_raw");

        // Old token must be revoked after rotation
        activeToken.IsRevoked.Should().BeTrue();
    }

    // ── Token not found ───────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_UnknownToken_ReturnsFailure()
    {
        _tokenRepo.GetByHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((RefreshToken?)null);

        var result = await _sut.Handle(new RefreshTokenCommand("ghost_token"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    // ── Expired active token ──────────────────────────────────────────────────

    [Fact]
    public async Task Handle_ExpiredToken_ReturnsFailure_WithoutIssuingNew()
    {
        const string rawToken = "expired_raw_token";
        var tokenHash = ComputeHash(rawToken);

        var user = CreateVerifiedUser();
        // Add token that will expire in the past by using reflection to set expiry
        var expiredToken = user.AddRefreshToken(tokenHash, DateTimeOffset.UtcNow.AddDays(-1));

        _tokenRepo.GetByHashAsync(tokenHash, Arg.Any<CancellationToken>()).Returns(expiredToken);

        var result = await _sut.Handle(new RefreshTokenCommand(rawToken), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        _jwtService.DidNotReceive().GenerateRefreshToken();
    }

    // ── Token theft detection ─────────────────────────────────────────────────

    [Fact]
    public async Task Handle_RevokedToken_RevokesAllUserSessionsAndReturnsFailure()
    {
        // Arrange: simulate an attacker replaying an already-rotated refresh token.
        const string stolenRawToken = "stolen_token";
        var stolenHash = ComputeHash(stolenRawToken);

        var compromisedUser = CreateVerifiedUser();

        // Add the stolen token and immediately revoke it (simulates it being rotated once already)
        var stolenToken = compromisedUser.AddRefreshToken(stolenHash, DateTimeOffset.UtcNow.AddDays(30));
        compromisedUser.RevokeRefreshToken(stolenToken.Id);
        stolenToken.IsRevoked.Should().BeTrue(); // sanity check

        // Add another active session to verify it also gets nuked
        compromisedUser.AddRefreshToken("other_session_hash", DateTimeOffset.UtcNow.AddDays(30));

        _tokenRepo.GetByHashAsync(stolenHash, Arg.Any<CancellationToken>()).Returns(stolenToken);
        _userRepo.GetByIdAsync(stolenToken.UserId, Arg.Any<CancellationToken>()).Returns(compromisedUser);

        // Act
        var result = await _sut.Handle(new RefreshTokenCommand(stolenRawToken), CancellationToken.None);

        // Assert — failure returned, all sessions revoked, user updated
        result.IsFailure.Should().BeTrue();

        compromisedUser.RefreshTokens.Should().AllSatisfy(t => t.IsRevoked.Should().BeTrue(),
            because: "all sessions must be terminated when token theft is detected");

        _userRepo.Received(1).Update(
            Arg.Is<User>(u => u.Id == compromisedUser.Id));

        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());

        // No new access token must be issued
        _jwtService.DidNotReceive().GenerateAccessToken(
            Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<UserRole>());
    }
}
