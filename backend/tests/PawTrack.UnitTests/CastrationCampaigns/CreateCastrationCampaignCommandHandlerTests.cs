using FluentAssertions;
using NSubstitute;
using PawTrack.Application.CastrationCampaigns.Commands.CreateCastrationCampaign;
using PawTrack.Application.CastrationCampaigns.Interfaces;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Auth;
using PawTrack.Domain.CastrationCampaigns;

namespace PawTrack.UnitTests.CastrationCampaigns;

public sealed class CreateCastrationCampaignCommandHandlerTests
{
    private readonly ICastrationCampaignRepository _repository = Substitute.For<ICastrationCampaignRepository>();
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private CreateCastrationCampaignCommandHandler CreateSut() =>
        new(_repository, _userRepository, _unitOfWork);

    private static CreateCastrationCampaignCommand CreateCommand(Guid requestingUserId) => new(
        RequestingUserId: requestingUserId,
        ExecutingClinicId: Guid.NewGuid(),
        Title: "Jornada de esterilizacion Cartago",
        VenueLabel: "Centro Civico",
        Canton: "Cartago",
        Latitude: 9.8644,
        Longitude: -83.9194,
        StartsAt: DateTimeOffset.UtcNow.AddDays(15),
        EndsAt: DateTimeOffset.UtcNow.AddDays(15).AddHours(8),
        ReservationsOpenAt: DateTimeOffset.UtcNow.AddDays(1),
        ReservationsCloseAt: DateTimeOffset.UtcNow.AddDays(13),
        Capacity: 80,
        BasePriceCrc: 15_000m,
        ConsentVersion: "v1");

    [Fact]
    public async Task Handle_WhenRequesterIsOwner_ReturnsForbiddenFailure()
    {
        var (owner, _) = User.Create("owner@pawtrack.cr", "hash", "Owner");
        _userRepository.GetByIdAsync(owner.Id, Arg.Any<CancellationToken>()).Returns(owner);

        var result = await CreateSut().Handle(CreateCommand(owner.Id), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain("Only clinics, municipalities, allies, or administrators can create campaigns.");
        await _repository.DidNotReceive().AddAsync(Arg.Any<CastrationCampaign>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenRequesterIsClinic_PersistsDraftCampaign()
    {
        var (clinicUser, _) = User.Create("clinic@pawtrack.cr", "hash", "Clinic");
        clinicUser.AssignClinicRole();
        _userRepository.GetByIdAsync(clinicUser.Id, Arg.Any<CancellationToken>()).Returns(clinicUser);

        var result = await CreateSut().Handle(CreateCommand(clinicUser.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be(CastrationCampaignStatus.Draft.ToString());
        result.Value.Capacity.Should().Be(80);
        await _repository.Received(1).AddAsync(Arg.Is<CastrationCampaign>(x =>
            x.OrganizerUserId == clinicUser.Id && x.Status == CastrationCampaignStatus.Draft), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenRequesterIsAdmin_PersistsDraftCampaign()
    {
        var (admin, _) = User.Create("admin@pawtrack.cr", "hash", "Admin");
        admin.PromoteToAdmin();
        _userRepository.GetByIdAsync(admin.Id, Arg.Any<CancellationToken>()).Returns(admin);

        var result = await CreateSut().Handle(CreateCommand(admin.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be(CastrationCampaignStatus.Draft.ToString());
        await _repository.Received(1).AddAsync(
            Arg.Is<CastrationCampaign>(x => x.OrganizerUserId == admin.Id),
            Arg.Any<CancellationToken>());
    }
}
