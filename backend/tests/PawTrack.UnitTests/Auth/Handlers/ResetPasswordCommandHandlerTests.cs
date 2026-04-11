using FluentAssertions;
using NSubstitute;
using PawTrack.Application.Auth.Commands.ResetPassword;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Auth;

namespace PawTrack.UnitTests.Auth.Handlers;

public sealed class ResetPasswordCommandHandlerTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly ResetPasswordCommandHandler _sut;

    public ResetPasswordCommandHandlerTests()
    {
        _sut = new ResetPasswordCommandHandler(_userRepository, _passwordHasher, _unitOfWork);
    }

    [Fact]
    public async Task Handle_WithValidToken_ResetsPassword()
    {
        var (user, _) = User.Create("user@example.com", "old_hash", "User");
        var rawResetToken = user.IssuePasswordResetToken();

        _userRepository.GetByPasswordResetTokenAsync(User.ToHexHash(rawResetToken), Arg.Any<CancellationToken>())
            .Returns(user);
        _passwordHasher.Hash("NewPass1!").Returns("new_hash");

        var result = await _sut.Handle(new ResetPasswordCommand(rawResetToken, "NewPass1!"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        user.PasswordHash.Should().Be("new_hash");
        user.PasswordResetToken.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WithUnknownToken_ReturnsFailure()
    {
        _userRepository.GetByPasswordResetTokenAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((User?)null);

        var result = await _sut.Handle(new ResetPasswordCommand("invalid-token", "NewPass1!"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }
}
