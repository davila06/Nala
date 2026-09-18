using FluentAssertions;
using NSubstitute;
using PawTrack.Application.CastrationCampaigns.Commands.ManageCastrationCampaign;
using PawTrack.Application.CastrationCampaigns.Interfaces;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Auth;
using PawTrack.Domain.CastrationCampaigns;

namespace PawTrack.UnitTests.CastrationCampaigns;

public sealed class CastrationCampaignLifecycleCommandHandlerTests
{
    private readonly ICastrationCampaignRepository _repository = Substitute.For<ICastrationCampaignRepository>();
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private static CastrationCampaign Draft(Guid organizerId)
    {
        var now = DateTimeOffset.UtcNow;
        return CastrationCampaign.Create(
            organizerId, Guid.NewGuid(), "Jornada", "Centro", "Cartago", 9.86, -83.91,
            now.AddDays(10), now.AddDays(10).AddHours(8), now.AddDays(1), now.AddDays(8),
            50, 10_000m, "v1");
    }

    [Fact]
    public async Task Submit_WhenRequesterOwnsCampaign_MovesToPendingApproval()
    {
        var organizerId = Guid.NewGuid();
        var campaign = Draft(organizerId);
        _repository.GetByIdAsync(campaign.Id, Arg.Any<CancellationToken>()).Returns(campaign);

        var sut = new SubmitCastrationCampaignCommandHandler(_repository, _unitOfWork);
        var result = await sut.Handle(new SubmitCastrationCampaignCommand(campaign.Id, organizerId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        campaign.Status.Should().Be(CastrationCampaignStatus.PendingApproval);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Approve_WhenRequesterIsNotAdmin_ReturnsFailure()
    {
        var requesterId = Guid.NewGuid();
        var campaign = Draft(Guid.NewGuid());
        campaign.SubmitForApproval();
        var (owner, _) = User.Create("owner@test.cr", "hash", "Owner");
        _repository.GetByIdAsync(campaign.Id, Arg.Any<CancellationToken>()).Returns(campaign);
        _userRepository.GetByIdAsync(requesterId, Arg.Any<CancellationToken>()).Returns(owner);

        var sut = new ApproveCastrationCampaignCommandHandler(_repository, _userRepository, _unitOfWork);
        var result = await sut.Handle(new ApproveCastrationCampaignCommand(campaign.Id, requesterId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain("Only administrators can approve campaigns.");
        campaign.Status.Should().Be(CastrationCampaignStatus.PendingApproval);
    }
}
