using FluentAssertions;
using NSubstitute;
using PawTrack.Application.Auth.Commands.ChangePassword;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Auth;

namespace PawTrack.UnitTests.Auth.Handlers;

public sealed class ChangePasswordCommandHandlerTests
{
    private readonly IUserRepository _userRepo = Substitute.For<IUserRepository>();
    private readonly IPasswordHasher _hasher = Substitute.For<IPasswordHasher>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly ChangePasswordCommandHandler _sut;

    public ChangePasswordCommandHandlerTests()
    {
        _sut = new ChangePasswordCommandHandler(_userRepo, _hasher, _uow);
    }

    [Fact]
    public async Task Handle_CorrectCurrentPassword_UpdatesHash()
    {
        var (user, token) = User.Create("u@test.com", "current-hash", "User");
        user.VerifyEmail(token);

        _userRepo.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        // Correct: handler calls Verify with plaintext + stored hash
        _hasher.Verify("current", "current-hash").Returns(true);
        _hasher.Hash("newpass8").Returns("new-hash");
        _uow.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

        var result = await _sut.Handle(
            new ChangePasswordCommand(user.Id, "current", "newpass8"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        user.PasswordHash.Should().Be("new-hash");
        _userRepo.Received(1).Update(user);
    }

    [Fact]
    public async Task Handle_WrongCurrentPassword_ReturnsFailure()
    {
        var (user, token) = User.Create("u@test.com", "correct-hash", "User");
        user.VerifyEmail(token);

        _userRepo.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _hasher.Verify("wrong", "correct-hash").Returns(false);

        var result = await _sut.Handle(
            new ChangePasswordCommand(user.Id, "wrong", "newpass8"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        _userRepo.DidNotReceive().Update(Arg.Any<User>());
        await _uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UserNotFound_ReturnsFailure()
    {
        _userRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((User?)null);

        var result = await _sut.Handle(
            new ChangePasswordCommand(Guid.NewGuid(), "any", "newpass8"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Contains("not found"));
    }
}
