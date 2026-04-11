using FluentAssertions;
using NSubstitute;
using PawTrack.Application.Auth.Commands.Logout;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Auth;

namespace PawTrack.UnitTests.Auth.Handlers;

public sealed class LogoutCommandHandlerTests
{
    private readonly IRefreshTokenRepository _tokenRepo = Substitute.For<IRefreshTokenRepository>();
    private readonly IUserRepository _userRepo        = Substitute.For<IUserRepository>();
    private readonly IJtiBlocklist _jtiBlocklist       = Substitute.For<IJtiBlocklist>();
    private readonly IUnitOfWork _uow                 = Substitute.For<IUnitOfWork>();

    private readonly LogoutCommandHandler _sut;

    public LogoutCommandHandlerTests()
    {
        _sut = new LogoutCommandHandler(_tokenRepo, _userRepo, _jtiBlocklist, _uow);
    }

    private static User CreateVerifiedUser()
    {
        var (user, rawToken) = User.Create("user@example.com", "hashed", "Test User");
        user.VerifyEmail(rawToken);
        return user;
    }

    private static string ComputeHash(string token)
    {
        var bytes = System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    [Fact]
    public async Task Handle_ValidLogout_RevokesRefreshTokenAndBlocklistsJti()
    {
        const string raw = "raw_refresh";
        const string jti = "some-jti-guid";
        var tokenHash = ComputeHash(raw);
        var user = CreateVerifiedUser();
        var activeToken = user.AddRefreshToken(tokenHash, DateTimeOffset.UtcNow.AddDays(30));

        _tokenRepo.GetActiveByHashAsync(tokenHash, Arg.Any<CancellationToken>())
            .Returns(activeToken);
        _userRepo.GetByIdAsync(user.Id, Arg.Any<CancellationToken>())
            .Returns(user);
        _uow.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

        var cmd = new LogoutCommand(user.Id, raw, jti, DateTimeOffset.UtcNow.AddMinutes(15));

        var result = await _sut.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        // refresh token must be revoked
        activeToken.IsRevoked.Should().BeTrue();

        // access token jti must be blocklisted
        await _jtiBlocklist.Received(1).AddAsync(
            jti,
            Arg.Any<DateTimeOffset>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NoJti_SkipsBlocklisting()
    {
        const string raw = "raw_refresh";
        var tokenHash = ComputeHash(raw);
        var user = CreateVerifiedUser();
        var activeToken = user.AddRefreshToken(tokenHash, DateTimeOffset.UtcNow.AddDays(30));

        _tokenRepo.GetActiveByHashAsync(tokenHash, Arg.Any<CancellationToken>())
            .Returns(activeToken);
        _userRepo.GetByIdAsync(user.Id, Arg.Any<CancellationToken>())
            .Returns(user);

        // No jti — e.g. an old token without the claim
        var cmd = new LogoutCommand(user.Id, raw, null, null);

        var result = await _sut.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _jtiBlocklist.DidNotReceive().AddAsync(
            Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>());
    }
}
