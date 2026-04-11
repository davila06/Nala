using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using PawTrack.Application.Auth.Commands.ForgotPassword;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Auth;

namespace PawTrack.UnitTests.Auth.Handlers;

public sealed class ForgotPasswordCommandHandlerTests
{
    private readonly IUserRepository _userRepo = Substitute.For<IUserRepository>();
    private readonly IEmailSender _emailSender = Substitute.For<IEmailSender>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly ILogger<ForgotPasswordCommandHandler> _logger = Substitute.For<ILogger<ForgotPasswordCommandHandler>>();

    private readonly ForgotPasswordCommandHandler _sut;

    public ForgotPasswordCommandHandlerTests()
    {
        _sut = new ForgotPasswordCommandHandler(_userRepo, _emailSender, _uow, _logger);
    }

    [Fact]
    public async Task Handle_UserExists_GeneratesTokenAndSendsResetEmail()
    {
        var (user, verificationToken) = User.Create("owner@example.com", "hash", "Owner");
        user.VerifyEmail(verificationToken);

        _userRepo.GetByEmailAsync(user.Email, Arg.Any<CancellationToken>()).Returns(user);

        var result = await _sut.Handle(new ForgotPasswordCommand(user.Email), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        user.PasswordResetToken.Should().NotBeNull();

        await _emailSender.Received(1).SendPasswordResetAsync(
            user.Email,
            user.Name,
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UserDoesNotExist_ReturnsSuccessWithoutSendingEmail()
    {
        _userRepo.GetByEmailAsync("ghost@example.com", Arg.Any<CancellationToken>()).Returns((User?)null);

        var result = await _sut.Handle(new ForgotPasswordCommand("ghost@example.com"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _emailSender.DidNotReceive().SendPasswordResetAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }
}
