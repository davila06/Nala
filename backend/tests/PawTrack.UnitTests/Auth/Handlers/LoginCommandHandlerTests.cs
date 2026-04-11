using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using PawTrack.Application.Auth.Commands.Login;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Auth;

namespace PawTrack.UnitTests.Auth.Handlers;

public sealed class LoginCommandHandlerTests
{
    private readonly IUserRepository _userRepo = Substitute.For<IUserRepository>();
    private readonly IPasswordHasher _hasher = Substitute.For<IPasswordHasher>();
    private readonly IJwtTokenService _jwtService = Substitute.For<IJwtTokenService>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly ILogger<LoginCommandHandler> _logger = Substitute.For<ILogger<LoginCommandHandler>>();

    private readonly LoginCommandHandler _sut;

    public LoginCommandHandlerTests()
    {
        _sut = new LoginCommandHandler(_userRepo, _hasher, _jwtService, _uow, _logger);
    }

    [Fact]
    public async Task Handle_ValidCredentials_ReturnsTokens()
    {
        // Arrange
        var (user, rawToken) = User.Create("user@example.com", "hashed", "Test User");
        // Verify email so login succeeds
        user.VerifyEmail(rawToken);

        var cmd = new LoginCommand("user@example.com", "correctpassword");
        _userRepo.GetByEmailAsync(cmd.Email, Arg.Any<CancellationToken>()).Returns(user);
        _hasher.Verify(cmd.Password, user.PasswordHash).Returns(true);
        _jwtService.GenerateAccessToken(user.Id, user.Email, user.Name, user.Role).Returns("access_token");
        _jwtService.GenerateRefreshToken().Returns(("raw_refresh", "hash_refresh"));
        _jwtService.AccessTokenExpirySeconds.Returns(900);
        _uow.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

        // Act
        var result = await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.AccessToken.Should().Be("access_token");
        result.Value.RefreshToken.Should().Be("raw_refresh");
        result.Value.ExpiresIn.Should().Be(900);
        result.Value.User.IsAdmin.Should().BeFalse(
            because: "a newly-created Owner user must not be granted admin capability");
    }

    [Fact]
    public async Task Handle_UserNotFound_ReturnsFailure()
    {
        var cmd = new LoginCommand("ghost@example.com", "password");
        _userRepo.GetByEmailAsync(cmd.Email, Arg.Any<CancellationToken>()).Returns((User?)null);

        var result = await _sut.Handle(cmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Contains("Invalid email or password"));
    }

    [Fact]
    public async Task Handle_EmailNotVerified_ReturnsFailure()
    {
        var (user, _) = User.Create("user@example.com", "hashed", "Test");
        var cmd = new LoginCommand("user@example.com", "password");
        _userRepo.GetByEmailAsync(cmd.Email, Arg.Any<CancellationToken>()).Returns(user);
        _hasher.Verify(cmd.Password, user.PasswordHash).Returns(true);

        var result = await _sut.Handle(cmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Contains("not yet verified"));
    }
}
