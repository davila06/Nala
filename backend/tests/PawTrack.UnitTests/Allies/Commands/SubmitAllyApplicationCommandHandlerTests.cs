using FluentAssertions;
using NSubstitute;
using PawTrack.Application.Allies.Commands.SubmitAllyApplication;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Allies;
using PawTrack.Domain.Auth;

namespace PawTrack.UnitTests.Allies.Commands;

public sealed class SubmitAllyApplicationCommandHandlerTests
{
    private readonly IAllyProfileRepository _allyProfileRepository = Substitute.For<IAllyProfileRepository>();
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly SubmitAllyApplicationCommandHandler _sut;

    public SubmitAllyApplicationCommandHandlerTests()
    {
        _sut = new SubmitAllyApplicationCommandHandler(_allyProfileRepository, _userRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_NewApplication_CreatesPendingProfile()
    {
        var (user, _) = User.Create("ally@test.com", "hashed-password", "Clinica San Jose");
        var command = new SubmitAllyApplicationCommand(
            user.Id,
            "Clinica San Jose",
            AllyType.VeterinaryClinic,
            "San Pedro centro",
            9.9354,
            -84.0512,
            2500);

        _userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _allyProfileRepository.GetByUserIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns((AllyProfile?)null);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.VerificationStatus.Should().Be(nameof(AllyVerificationStatus.Pending));
        result.Value.OrganizationName.Should().Be("Clinica San Jose");

        await _allyProfileRepository.Received(1).AddAsync(
            Arg.Is<AllyProfile>(profile =>
                profile.UserId == user.Id
                && profile.OrganizationName == "Clinica San Jose"
                && profile.CoverageLabel == "San Pedro centro"
                && profile.CoverageRadiusMetres == 2500),
            Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}