using FluentAssertions;
using NSubstitute;
using PawTrack.Application.Clinics.Commands.ManageClinicFinanceAccess;
using PawTrack.Application.Clinics.Interfaces;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Auth;
using PawTrack.Domain.Clinics;

namespace PawTrack.UnitTests.Clinics;

public sealed class ClinicFinanceMembershipTests
{
    [Fact]
    public async Task GrantRequiresOwnerWithMfa()
    {
        var (owner, _) = User.Create("owner@clinic.test", "hash", "Owner");
        var (staff, _) = User.Create("staff@clinic.test", "hash", "Staff");
        var clinic = Clinic.Create(owner.Id, "Clinica", "LIC-1", "San Jose", 9.93m, -84.08m, "clinic@test.cr");
        var clinics = Substitute.For<IClinicRepository>();
        clinics.GetByIdAsync(clinic.Id, Arg.Any<CancellationToken>()).Returns(clinic);
        var users = Substitute.For<IUserRepository>();
        users.GetByIdAsync(owner.Id, Arg.Any<CancellationToken>()).Returns(owner);
        users.GetByEmailAsync(staff.Email, Arg.Any<CancellationToken>()).Returns(staff);
        var access = Substitute.For<IClinicFinanceAccessRepository>();
        var handler = new GrantClinicFinanceMembershipCommandHandler(clinics, users, access,
            Substitute.For<IAuditLogRepository>(), Substitute.For<IUnitOfWork>());

        var result = await handler.Handle(new GrantClinicFinanceMembershipCommand(clinic.Id, owner.Id, staff.Email, ClinicFinanceRole.Cashier), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        await access.DidNotReceive().AddAsync(Arg.Any<ClinicFinanceMembership>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void CashierCannotVoidCloseOrIssueFiscalDocuments()
    {
        var membership = ClinicFinanceMembership.Grant(Guid.NewGuid(), Guid.NewGuid(), ClinicFinanceRole.Cashier, Guid.NewGuid());

        membership.Allows(ClinicFinancePermission.Collect).Should().BeTrue();
        membership.Allows(ClinicFinancePermission.Void).Should().BeFalse();
        membership.Allows(ClinicFinancePermission.CloseCash).Should().BeFalse();
        membership.Allows(ClinicFinancePermission.Fiscal).Should().BeFalse();
        membership.Revoke(Guid.NewGuid());
        membership.Allows(ClinicFinancePermission.Collect).Should().BeFalse();
    }
}
