using FluentAssertions;
using NSubstitute;
using PawTrack.Application.Clinics.Commands.ManageClinicBilling;
using PawTrack.Application.Clinics.Interfaces;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Certificates;
using PawTrack.Domain.Clinics;
using PawTrack.Domain.Auth;

namespace PawTrack.UnitTests.Clinics;

public sealed class ClinicSaleSecurityTests
{
    [Fact]
    public async Task CashierCanCreateSaleButCannotVoidIt()
    {
        var ownerId = Guid.NewGuid();
        var cashierId = Guid.NewGuid();
        var clinic = Clinic.Create(ownerId, "Clinica Test", "VET-123", "San Jose", 9.93m, -84.08m, "clinic@test.cr");
        var clinics = Substitute.For<IClinicRepository>();
        clinics.GetByIdAsync(clinic.Id, Arg.Any<CancellationToken>()).Returns(clinic);
        var access = Substitute.For<IClinicFinanceAccessRepository>();
        access.HasPermissionAsync(clinic.Id, cashierId, ClinicFinancePermission.Collect, Arg.Any<CancellationToken>()).Returns(true);
        var billing = Substitute.For<IClinicBillingRepository>();
        var sale = ClinicSale.Create(clinic.Id, cashierId, null, null, null, "REC-TEST");
        sale.AddLine("Consulta", ClinicSaleLineType.Service, 1, 1000m, null, null);
        billing.GetSaleByIdAsync(sale.Id, Arg.Any<CancellationToken>()).Returns(sale);
        var audit = Substitute.For<IAuditLogRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var create = new CreateClinicSaleCommandHandler(clinics, billing, audit, unitOfWork,
            Substitute.For<IVeterinarianAppointmentRepository>(), Substitute.For<IClinicalConsultationRepository>(), access);
        var voidSale = new VoidClinicSaleCommandHandler(clinics, billing, audit, unitOfWork, access, Substitute.For<IUserRepository>());

        var created = await create.Handle(new CreateClinicSaleCommand(clinic.Id, cashierId, null, null, null, "REC-2",
            [new ClinicSaleLineInput("Consulta", ClinicSaleLineType.Service, 1, 1000m, null, null)]), CancellationToken.None);
        var voided = await voidSale.Handle(new VoidClinicSaleCommand(clinic.Id, cashierId, sale.Id, "Anular"), CancellationToken.None);

        created.IsSuccess.Should().BeTrue();
        voided.IsFailure.Should().BeTrue();
        billing.DidNotReceive().UpdateSale(sale);

        var discounted = await create.Handle(new CreateClinicSaleCommand(clinic.Id, cashierId, null, null, null, "REC-DISC",
            [new ClinicSaleLineInput("Consulta", ClinicSaleLineType.Service, 1, 1000m, null, null)], 100m, "Cortesia"), CancellationToken.None);
        discounted.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task AdministratorWithoutMfa_CannotVoidSale()
    {
        var (administrator, _) = User.Create("admin@clinic.test", "hash", "Admin");
        var clinic = Clinic.Create(Guid.NewGuid(), "Clinica", "VET-1", "San Jose", 9.93m, -84.08m, "clinic@test.cr");
        var sale = ClinicSale.Create(clinic.Id, clinic.UserId, null, null, null, "REC-1");
        sale.AddLine("Consulta", ClinicSaleLineType.Service, 1, 1000m, null, null);
        var clinics = Substitute.For<IClinicRepository>();
        clinics.GetByIdAsync(clinic.Id, Arg.Any<CancellationToken>()).Returns(clinic);
        var access = Substitute.For<IClinicFinanceAccessRepository>();
        access.HasPermissionAsync(clinic.Id, administrator.Id, ClinicFinancePermission.Void, Arg.Any<CancellationToken>()).Returns(true);
        var users = Substitute.For<IUserRepository>();
        users.GetByIdAsync(administrator.Id, Arg.Any<CancellationToken>()).Returns(administrator);
        var billing = Substitute.For<IClinicBillingRepository>();
        billing.GetSaleByIdAsync(sale.Id, Arg.Any<CancellationToken>()).Returns(sale);
        var handler = new VoidClinicSaleCommandHandler(clinics, billing, Substitute.For<IAuditLogRepository>(), Substitute.For<IUnitOfWork>(), access, users);

        var result = await handler.Handle(new VoidClinicSaleCommand(clinic.Id, administrator.Id, sale.Id, "Error"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        sale.Status.Should().NotBe(ClinicSaleStatus.Voided);
    }

    [Fact]
    public async Task CreateSale_RejectsAppointmentFromAnotherClinic()
    {
        var userId = Guid.NewGuid();
        var clinic = Clinic.Create(userId, "Clinica Test", "VET-123", "San Jose", 9.93m, -84.08m, "clinic@test.cr");
        var appointment = VeterinarianAppointment.Schedule(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow.AddDays(1), TimeSpan.FromMinutes(30));
        var clinics = Substitute.For<IClinicRepository>();
        clinics.GetByIdAsync(clinic.Id, Arg.Any<CancellationToken>()).Returns(clinic);
        var appointments = Substitute.For<IVeterinarianAppointmentRepository>();
        appointments.GetByIdAsync(appointment.Id, Arg.Any<CancellationToken>()).Returns(appointment);
        var billing = Substitute.For<IClinicBillingRepository>();
        var handler = new CreateClinicSaleCommandHandler(clinics, billing, Substitute.For<PawTrack.Application.Common.Interfaces.IAuditLogRepository>(), Substitute.For<IUnitOfWork>(), appointments, Substitute.For<IClinicalConsultationRepository>(), Substitute.For<IClinicFinanceAccessRepository>());

        var result = await handler.Handle(new CreateClinicSaleCommand(clinic.Id, userId, appointment.Id, null, appointment.PetId, "REC-1", [new ClinicSaleLineInput("Consulta", ClinicSaleLineType.Service, 1, 10000m, null, null)]), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        await billing.DidNotReceive().AddSaleAsync(Arg.Any<ClinicSale>(), Arg.Any<CancellationToken>());
    }
}
