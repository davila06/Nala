using FluentAssertions;
using NSubstitute;
using PawTrack.Application.CastrationCampaigns.Interfaces;
using PawTrack.Application.CastrationCampaigns.Queries.GetCastrationAppointments;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Auth;

namespace PawTrack.UnitTests.CastrationCampaigns;

public sealed class GetCampaignAppointmentsQueryHandlerTests
{
    [Fact]
    public async Task Handle_WithoutActiveClinicSite_DeniesBeforeLoadingCampaign()
    {
        var userId = Guid.NewGuid();
        var campaigns = Substitute.For<ICastrationCampaignRepository>();
        var appointments = Substitute.For<ICastrationAppointmentRepository>();
        var clinics = Substitute.For<IClinicRepository>();
        var users = Substitute.For<IUserRepository>();
        var siteContext = Substitute.For<IActiveClinicSiteContext>();
        siteContext.UserId.Returns(userId);

        var handler = new GetCampaignAppointmentsQueryHandler(
            campaigns, appointments, clinics, users, siteContext);

        var result = await handler.Handle(
            new GetCampaignAppointmentsQuery(Guid.NewGuid(), userId, 1, 20), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        await campaigns.DidNotReceive().GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await appointments.DidNotReceive().GetByCampaignPagedAsync(
            Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }
}
