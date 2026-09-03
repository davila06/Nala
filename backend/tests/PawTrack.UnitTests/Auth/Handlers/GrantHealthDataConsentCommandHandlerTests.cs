using FluentAssertions;
using NSubstitute;
using PawTrack.Application.Auth.Commands.GrantHealthDataConsent;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Auth;

namespace PawTrack.UnitTests.Auth.Handlers;

public sealed class GrantHealthDataConsentCommandHandlerTests
{
    private readonly IUserRepository _userRepo = Substitute.For<IUserRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    private readonly GrantHealthDataConsentCommandHandler _sut;

    public GrantHealthDataConsentCommandHandlerTests()
    {
        _sut = new GrantHealthDataConsentCommandHandler(_userRepo, _uow);
    }

    [Fact]
    public async Task Handle_UserNotFound_ReturnsFailure()
    {
        _userRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((User?)null);

        var result = await _sut.Handle(new GrantHealthDataConsentCommand(Guid.NewGuid()), default);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ValidUser_GrantsConsentAndPersists()
    {
        var (user, _) = User.Create("owner@test.com", "hash", "Owner");
        _userRepo.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        var result = await _sut.Handle(new GrantHealthDataConsentCommand(user.Id), default);

        result.IsSuccess.Should().BeTrue();
        user.HasHealthDataConsent.Should().BeTrue();
        _userRepo.Received(1).Update(user);
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AlreadyConsented_IsIdempotent_DoesNotOverwriteTimestamp()
    {
        var (user, _) = User.Create("owner@test.com", "hash", "Owner");
        user.GrantHealthDataConsent();
        var originalTimestamp = user.HealthDataConsentedAt;
        _userRepo.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        var result = await _sut.Handle(new GrantHealthDataConsentCommand(user.Id), default);

        result.IsSuccess.Should().BeTrue();
        user.HealthDataConsentedAt.Should().Be(originalTimestamp);
    }
}
