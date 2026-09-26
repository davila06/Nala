using FluentAssertions;
using PawTrack.Domain.Clinics;
using NSubstitute;
using PawTrack.Application.Clinics.Commands.ManageClinicStaff;
using PawTrack.Application.Clinics.Interfaces;
using PawTrack.Application.Certificates.Interfaces;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Auth;
using PawTrack.Domain.Certificates;

namespace PawTrack.UnitTests.Clinics;

public sealed class ClinicStaffPermissionTests
{
    [Fact]
    public void Receptionist_CanManageAgendaButCannotWriteMedicalRecords()
    {
        var member = ClinicStaffMembership.Grant(Guid.NewGuid(), Guid.NewGuid(), ClinicStaffRole.Receptionist, Guid.NewGuid());

        member.Allows(ClinicStaffPermission.ManageAgenda).Should().BeTrue();
        member.Allows(ClinicStaffPermission.WriteMedical).Should().BeFalse();
        member.Revoke(Guid.NewGuid());
        member.Allows(ClinicStaffPermission.ManageAgenda).Should().BeFalse();
    }

    [Fact]
    public void Assistant_CanWorkPreparationAndInventoryTasksWithoutClientCommunicationAccess()
    {
        var member = ClinicStaffMembership.Grant(Guid.NewGuid(), Guid.NewGuid(), ClinicStaffRole.Assistant, Guid.NewGuid());

        member.CanWorkCrmTask(ClinicCrmTaskType.PrepareConsultation).Should().BeTrue();
        member.CanWorkCrmTask(ClinicCrmTaskType.ReviewInventory).Should().BeTrue();
        member.CanWorkCrmTask(ClinicCrmTaskType.FollowUpTreatment).Should().BeFalse();
        member.Allows(ClinicStaffPermission.ViewCrm).Should().BeFalse();
    }

    [Fact]
    public void FinanceRoles_AreRestrictedToTheirOperationalTaskScope()
    {
        var clinicId = Guid.NewGuid();
        var grantorId = Guid.NewGuid();
        var cashier = ClinicFinanceMembership.Grant(clinicId, Guid.NewGuid(), ClinicFinanceRole.Cashier, grantorId);
        var manager = ClinicFinanceMembership.Grant(clinicId, Guid.NewGuid(), ClinicFinanceRole.Administrator, grantorId);

        cashier.CanWorkCrmTask(ClinicCrmTaskType.CollectPayment).Should().BeTrue();
        cashier.CanWorkCrmTask(ClinicCrmTaskType.CloseCash).Should().BeFalse();
        manager.CanWorkCrmTask(ClinicCrmTaskType.CloseCash).Should().BeTrue();
        manager.CanWorkCrmTask(ClinicCrmTaskType.ReviewOperations).Should().BeTrue();
    }

    [Fact]
    public async Task OwnerCannotGrantForeignVeterinarianAsClinicalStaff()
    {
        var (owner, _) = User.Create("clinic@pawtrack.cr", "hash", "Owner");
        owner.ConfigureMfa("protected");
        var (staff, token) = User.Create("staff@pawtrack.cr", "hash", "Staff");
        staff.VerifyEmail(token);
        var clinic = Clinic.Create(owner.Id, "Clinica", "LIC-1", "San Jose", 9.93m, -84.08m, owner.Email);
        var foreignVet = ClinicVeterinarian.Create(Guid.NewGuid(), "Dr. Ajeno", "VET-999");
        var clinics = Substitute.For<IClinicRepository>();
        clinics.GetByIdAsync(clinic.Id, Arg.Any<CancellationToken>()).Returns(clinic);
        var users = Substitute.For<IUserRepository>();
        users.GetByIdAsync(owner.Id, Arg.Any<CancellationToken>()).Returns(owner);
        users.GetByEmailAsync(staff.Email, Arg.Any<CancellationToken>()).Returns(staff);
        var veterinarians = Substitute.For<IClinicVeterinarianRepository>();
        veterinarians.GetByIdAsync(foreignVet.Id, Arg.Any<CancellationToken>()).Returns(foreignVet);
        var access = Substitute.For<IClinicStaffAccessRepository>();
        var handler = new GrantClinicStaffMembershipCommandHandler(clinics, users, veterinarians, access,
            Substitute.For<IAuditLogRepository>(), Substitute.For<IUnitOfWork>());

        var result = await handler.Handle(new GrantClinicStaffMembershipCommand(clinic.Id, owner.Id, staff.Email,
            ClinicStaffRole.Veterinarian, foreignVet.Id), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        await access.DidNotReceive().AddAsync(Arg.Any<ClinicStaffMembership>(), Arg.Any<CancellationToken>());
    }
}
