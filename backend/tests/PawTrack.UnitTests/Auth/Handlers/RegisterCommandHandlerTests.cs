using NSubstitute;
using PawTrack.Application.Auth.Commands.Register;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Auth;
using FluentAssertions;
using Microsoft.Extensions.Logging;

namespace PawTrack.UnitTests.Auth.Handlers;

public sealed class RegisterCommandHandlerTests
{
    private readonly IUserRepository _userRepo = Substitute.For<IUserRepository>();
    private readonly IPasswordHasher _hasher = Substitute.For<IPasswordHasher>();
    private readonly IEmailSender _emailSender = Substitute.For<IEmailSender>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly ILogger<RegisterCommandHandler> _logger = Substitute.For<ILogger<RegisterCommandHandler>>();

    private readonly RegisterCommandHandler _sut;

    public RegisterCommandHandlerTests()
    {
        _sut = new RegisterCommandHandler(_userRepo, _hasher, _emailSender, _uow, _logger);
    }

    [Fact]
    public async Task Handle_NewUser_CreatesUserAndSendsEmail()
    {
        // Arrange
        var cmd = new RegisterCommand("Test User", "test@example.com", "Password1");
        _userRepo.ExistsByEmailAsync(cmd.Email, Arg.Any<CancellationToken>()).Returns(false);
        _hasher.Hash(cmd.Password).Returns("hashed_password");
        _uow.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

        // Act
        var result = await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNullOrEmpty();

        await _userRepo.Received(1).AddAsync(
            Arg.Is<User>(u => u.Email == cmd.Email.ToLowerInvariant()),
            Arg.Any<CancellationToken>());

        await _emailSender.Received(1).SendEmailVerificationAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DuplicateEmail_ReturnsSilentSuccess_WithoutCreatingUser()
    {
        // Arrange — anti-enumeration: the handler must NOT surface whether
        // an account for this email already exists.
        var cmd = new RegisterCommand("Test User", "existing@example.com", "Password1");
        _userRepo.ExistsByEmailAsync(cmd.Email, Arg.Any<CancellationToken>()).Returns(true);

        // Act
        var result = await _sut.Handle(cmd, CancellationToken.None);

        // Assert — success shape, no user persisted, no verification email sent
        result.IsSuccess.Should().BeTrue();

        await _userRepo.DidNotReceive().AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
        await _emailSender.DidNotReceive().SendEmailVerificationAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
