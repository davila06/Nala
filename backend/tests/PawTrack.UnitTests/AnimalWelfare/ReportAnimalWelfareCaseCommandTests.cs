using FluentAssertions;
using NSubstitute;
using PawTrack.Application.AnimalWelfare.Commands;
using PawTrack.Application.AnimalWelfare.Interfaces;
using PawTrack.Application.AnimalWelfare.Routing;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Municipalities.Interfaces;
using PawTrack.Domain.Allies;
using PawTrack.Domain.AnimalWelfare;

namespace PawTrack.UnitTests.AnimalWelfare;

public sealed class ReportAnimalWelfareCaseCommandTests
{
    [Fact]
    public async Task Auto_routing_stores_suggestion_without_assigning_or_referring_case()
    {
        var ally = AllyProfile.Create(Guid.NewGuid(), "Refugio verificado", AllyType.Shelter, "Heredia", 9.93, -84.08, 2000);
        ally.Approve();
        var cases = Substitute.For<IAnimalWelfareCaseRepository>();
        var audits = Substitute.For<IAnimalWelfareAuditRepository>();
        var allies = Substitute.For<IAllyProfileRepository>();
        var municipalities = Substitute.For<IMunicipalProfileRepository>();
        var scrubber = Substitute.For<IPiiScrubber>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        AnimalWelfareCase? persistedCase = null;

        scrubber.Scrub(Arg.Any<string>()).Returns("Hechos observables reportados");
        allies.GetVerifiedCoveringPointAsync(9.93, -84.08, Arg.Any<CancellationToken>())
            .Returns(new[] { ally });
        municipalities.GetAllActiveAsync(Arg.Any<CancellationToken>()).Returns([]);
        cases.AddAsync(Arg.Do<AnimalWelfareCase>(item => persistedCase = item), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var handler = new ReportAnimalWelfareCaseCommandHandler(
            cases,
            audits,
            new WelfareRoutingService(allies, municipalities),
            scrubber,
            unitOfWork);
        var result = await handler.Handle(
            new ReportAnimalWelfareCaseCommand(
                WelfareCaseType.SuspectedAbuse,
                WelfareSeverity.High,
                "Heredia",
                "Hechos observables reportados",
                null,
                true,
                9.93,
                -84.08,
                AutoRoutingRequested: true),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        persistedCase.Should().NotBeNull();
        persistedCase!.AutoRoutingRequested.Should().BeTrue();
        persistedCase.SuggestedOrganizationUserId.Should().Be(ally.UserId);
        persistedCase.AssignedOrganizationUserId.Should().BeNull();
        persistedCase.Status.Should().Be(WelfareCaseStatus.Received);
        await audits.DidNotReceiveWithAnyArgs().AddReferralAsync(default!);
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
