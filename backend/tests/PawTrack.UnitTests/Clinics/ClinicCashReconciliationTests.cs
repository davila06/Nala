using FluentAssertions;
using NSubstitute;
using PawTrack.Application.Clinics.Commands.ManageClinicBilling;
using PawTrack.Application.Clinics.Interfaces;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Clinics;

namespace PawTrack.UnitTests.Clinics;

public sealed class ClinicCashReconciliationTests
{
    [Fact]
    public async Task ClosingAnAlreadyClosedDay_ReturnsBusinessFailure()
    {
        var ownerId = Guid.NewGuid();
        var clinic = Clinic.Create(ownerId, "Clinica Test", "VET-1", "San Jose", 9.93m, -84.08m, "clinic@test.cr");
        var clinics = Substitute.For<IClinicRepository>();
        clinics.GetByIdAsync(clinic.Id, Arg.Any<CancellationToken>()).Returns(clinic);
        var billing = Substitute.For<IClinicBillingRepository>();
        var date = new DateOnly(2026, 9, 25);
        billing.HasCashCloseAsync(clinic.Id, date, Arg.Any<CancellationToken>()).Returns(true);
        var user = PawTrack.Domain.Auth.User.Create("owner@clinic.test", "hash", "Owner").User;
        user.ConfigureMfa("protected");
        var users = Substitute.For<IUserRepository>();
        users.GetByIdAsync(ownerId, Arg.Any<CancellationToken>()).Returns(user);
        var handler = new CloseClinicCashCommandHandler(clinics, billing, Substitute.For<IAuditLogRepository>(),
            Substitute.For<IUnitOfWork>(), Substitute.For<IClinicFinanceAccessRepository>(), users);

        var result = await handler.Handle(new CloseClinicCashCommand(clinic.Id, ownerId, date), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        await billing.DidNotReceive().AddCashCloseAsync(Arg.Any<ClinicCashClose>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CashClose_UsesNetMovementsIncludingRecordedRefunds()
    {
        var ownerId = Guid.NewGuid();
        var clinic = Clinic.Create(ownerId, "Clinica Test", "VET-1", "San Jose", 9.93m, -84.08m, "clinic@test.cr");
        var sale = ClinicSale.Create(clinic.Id, ownerId, null, null, null, "REC-1");
        sale.AddLine("Servicio", ClinicSaleLineType.Service, 1, 10000m, null, null);
        var payment = sale.RecordPayment(10000m, ClinicPaymentMethod.Cash, null, ownerId);
        var refund = sale.RecordRefund(payment.Id, 4000m, "Ajuste", "REF-1", ownerId);
        var clinics = Substitute.For<IClinicRepository>();
        clinics.GetByIdAsync(clinic.Id, Arg.Any<CancellationToken>()).Returns(clinic);
        var billing = Substitute.For<IClinicBillingRepository>();
        var date = DateOnly.FromDateTime(DateTime.UtcNow);
        billing.GetPaymentsForClinicOnDateAsync(clinic.Id, date, Arg.Any<CancellationToken>()).Returns([payment]);
        billing.GetRefundsForClinicOnDateAsync(clinic.Id, date, Arg.Any<CancellationToken>()).Returns([refund]);
        var user = PawTrack.Domain.Auth.User.Create("owner2@clinic.test", "hash", "Owner").User;
        user.ConfigureMfa("protected");
        var users = Substitute.For<IUserRepository>();
        users.GetByIdAsync(ownerId, Arg.Any<CancellationToken>()).Returns(user);
        var handler = new CloseClinicCashCommandHandler(clinics, billing, Substitute.For<IAuditLogRepository>(),
            Substitute.For<IUnitOfWork>(), Substitute.For<IClinicFinanceAccessRepository>(), users);

        var result = await handler.Handle(new CloseClinicCashCommand(clinic.Id, ownerId, date), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await billing.Received(1).AddCashCloseAsync(Arg.Is<ClinicCashClose>(close => close.TotalCrc == 6000m), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ServiceBreakdown_ExcludesVoidedSales()
    {
        var ownerId = Guid.NewGuid();
        var clinic = Clinic.Create(ownerId, "Clinica Test", "VET-1", "San Jose", 9.93m, -84.08m, "clinic@test.cr");
        var voidedSale = ClinicSale.Create(clinic.Id, ownerId, null, null, null, "REC-VOID");
        voidedSale.AddLine("Consulta", ClinicSaleLineType.Service, 1, 1000m, null, null);
        voidedSale.Void("Cancelada", ownerId);
        var clinics = Substitute.For<IClinicRepository>();
        clinics.GetByIdAsync(clinic.Id, Arg.Any<CancellationToken>()).Returns(clinic);
        var billing = Substitute.For<IClinicBillingRepository>();
        var date = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(-6));
        billing.GetSalesForClinicOnDateAsync(clinic.Id, date, Arg.Any<CancellationToken>()).Returns([voidedSale]);
        var handler = new GetClinicSalesReportQueryHandler(clinics, billing, Substitute.For<IClinicFinanceAccessRepository>());

        var result = await handler.Handle(new GetClinicSalesReportQuery(clinic.Id, ownerId, date), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.ByService.Should().BeEmpty();
    }

    [Fact]
    public async Task SalesReport_IncludesOutstandingBalancesAndExcludesVoidedSales()
    {
        var ownerId = Guid.NewGuid();
        var clinic = Clinic.Create(ownerId, "Clinica Test", "VET-1", "San Jose", 9.93m, -84.08m, "clinic@test.cr");
        var date = new DateOnly(2026, 9, 25);
        var openSale = ClinicSale.Create(clinic.Id, ownerId, null, null, null, "REC-OPEN");
        openSale.AddLine("Consulta", ClinicSaleLineType.Service, 1, 5000m, null, null);
        var partialSale = ClinicSale.Create(clinic.Id, ownerId, null, null, null, "REC-PARTIAL");
        partialSale.AddLine("Vacuna", ClinicSaleLineType.Service, 1, 10000m, null, null);
        partialSale.RecordPayment(2500m, ClinicPaymentMethod.Cash, null, ownerId);
        var voidedSale = ClinicSale.Create(clinic.Id, ownerId, null, null, null, "REC-VOID-BAL");
        voidedSale.AddLine("Anulada", ClinicSaleLineType.Service, 1, 9000m, null, null);
        voidedSale.Void("Anulada", ownerId);
        var clinics = Substitute.For<IClinicRepository>();
        clinics.GetByIdAsync(clinic.Id, Arg.Any<CancellationToken>()).Returns(clinic);
        var billing = Substitute.For<IClinicBillingRepository>();
        billing.GetSalesForClinicOnDateAsync(clinic.Id, date, Arg.Any<CancellationToken>())
            .Returns([openSale, partialSale, voidedSale]);
        var handler = new GetClinicSalesReportQueryHandler(clinics, billing, Substitute.For<IClinicFinanceAccessRepository>());

        var result = await handler.Handle(new GetClinicSalesReportQuery(clinic.Id, ownerId, date), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.PendingSaleCount.Should().Be(2);
        result.Value.PendingBalanceCrc.Should().Be(12500m);
    }
}
