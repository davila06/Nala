using FluentAssertions;
using NSubstitute;
using PawTrack.Application.Allies.Commands.ReviewAllyApplication;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Allies;
using PawTrack.Domain.Auth;

namespace PawTrack.UnitTests.Allies.Commands;

public sealed class ReviewAllyApplicationCommandHandlerTests
{
    private readonly IAllyProfileRepository _allyProfileRepository = Substitute.For<IAllyProfileRepository>();
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly ReviewAllyApplicationCommandHandler _sut;

    public ReviewAllyApplicationCommandHandlerTests()
    {
        _sut = new ReviewAllyApplicationCommandHandler(_allyProfileRepository, _userRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_ApproveApplication_VerifiesProfileAndPromotesUser()
    {
        var (user, _) = User.Create("ally@test.com", "hashed-password", "Refugio Norte");
        var profile = AllyProfile.Create(
            user.Id,
            "Refugio Norte",
            AllyType.Shelter,
            "Heredia centro",
            10.0024,
            -84.1165,
            3000);

        _userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _allyProfileRepository.GetByUserIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(profile);

        var result = await _sut.Handle(new ReviewAllyApplicationCommand(user.Id, true), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.VerificationStatus.Should().Be(nameof(AllyVerificationStatus.Verified));
        user.Role.Should().Be(UserRole.Ally);
        _allyProfileRepository.Received(1).Update(profile);
        _userRepository.Received(1).Update(user);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}