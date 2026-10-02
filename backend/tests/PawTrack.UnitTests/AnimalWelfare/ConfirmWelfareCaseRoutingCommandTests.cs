using FluentAssertions;
using NSubstitute;
using PawTrack.Application.AnimalWelfare.Commands;
using PawTrack.Application.AnimalWelfare.Interfaces;
using PawTrack.Application.AnimalWelfare.Routing;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Municipalities.Interfaces;
using PawTrack.Domain.AnimalWelfare;
using PawTrack.Domain.Allies;
using PawTrack.Domain.Municipalities;

namespace PawTrack.UnitTests.AnimalWelfare;

public sealed class ConfirmWelfareCaseRoutingCommandTests
{
    [Fact]
    public async Task Rejects_unverified_ally_and_keeps_case_unassigned()
    {
        var actor = Guid.NewGuid();
        var caseRecord = CreateCase(autoRoute: true);
        var cases = Substitute.For<IAnimalWelfareCaseRepository>();
        var audits = Substitute.For<IAnimalWelfareAuditRepository>();
        var allies = Substitute.For<IAllyProfileRepository>();
        var municipalities = Substitute.For<IMunicipalProfileRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        cases.GetByIdAsync(caseRecord.Id, Arg.Any<CancellationToken>()).Returns(caseRecord);
        allies.GetVerifiedCoveringPointAsync(Arg.Any<double>(), Arg.Any<double>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<AllyProfile>());

        var handler = new ConfirmWelfareCaseRoutingCommandHandler(
            cases,
            audits,
            new WelfareRoutingService(allies, municipalities),
            unitOfWork);
        var result = await handler.Handle(
            new ConfirmWelfareCaseRoutingCommand(caseRecord.Id, actor, Guid.NewGuid(), WelfareReferralRecipientType.Ally, "Asignar"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        caseRecord.AssignedOrganizationUserId.Should().BeNull();
        await audits.DidNotReceiveWithAnyArgs().AddReferralAsync(default!);
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Confirms_eligible_nearby_ally_only_after_admin_action()
    {
        var actor = Guid.NewGuid();
        var caseRecord = CreateCase(autoRoute: true);
        var ally = AllyProfile.Create(Guid.NewGuid(), "Refugio verificado", AllyType.Shelter, "Heredia", 9.93, -84.08, 2000);
        ally.Approve();
        var cases = Substitute.For<IAnimalWelfareCaseRepository>();
        var audits = Substitute.For<IAnimalWelfareAuditRepository>();
        var allies = Substitute.For<IAllyProfileRepository>();
        var municipalities = Substitute.For<IMunicipalProfileRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        cases.GetByIdAsync(caseRecord.Id, Arg.Any<CancellationToken>()).Returns(caseRecord);
        allies.GetVerifiedCoveringPointAsync(9.93, -84.08, Arg.Any<CancellationToken>()).Returns(new[] { ally });
        municipalities.GetAllActiveAsync(Arg.Any<CancellationToken>()).Returns(Array.Empty<MunicipalityProfile>());

        var handler = new ConfirmWelfareCaseRoutingCommandHandler(
            cases,
            audits,
            new WelfareRoutingService(allies, municipalities),
            unitOfWork);
        var result = await handler.Handle(
            new ConfirmWelfareCaseRoutingCommand(caseRecord.Id, actor, ally.UserId, WelfareReferralRecipientType.Ally, "Cobertura local"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        caseRecord.AssignedOrganizationUserId.Should().Be(ally.UserId);
        caseRecord.Status.Should().Be(WelfareCaseStatus.Assigned);
        await audits.Received(1).AddReferralAsync(Arg.Any<AnimalWelfareReferral>(), Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private static AnimalWelfareCase CreateCase(bool autoRoute) => AnimalWelfareCase.Create(
        WelfareCaseType.SuspectedAbuse,
        WelfareSeverity.High,
        "Heredia",
        "Reporte de hechos observables suficientes para crear el caso",
        null,
        true,
        9.93,
        -84.08,
        autoRoutingRequested: autoRoute);
}
