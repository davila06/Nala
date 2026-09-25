using FluentAssertions;
using PawTrack.Domain.Clinics;
using NSubstitute;
using PawTrack.Application.Clinics.Commands.ManageClinicBilling;
using PawTrack.Application.Clinics.Interfaces;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Auth;

namespace PawTrack.UnitTests.Clinics;

public sealed class ClinicFiscalSubmissionTests
{
    [Fact]
    public async Task MissingVerifiedIssuer_NeverContactsFiscalProvider()
    {
        var (owner, _) = User.Create("clinic@test.cr", "hash", "Owner");
        owner.ConfigureMfa("protected");
        var clinic = Clinic.Create(owner.Id, "Clinica", "LIC-1", "San Jose", 9.93m, -84.08m, owner.Email);
        var sale = ClinicSale.Create(clinic.Id, owner.Id, null, null, null, "REC-1");
        sale.AddLine("Consulta", ClinicSaleLineType.Service, 1, 1000m, null, null);
        sale.RecordPayment(1000m, ClinicPaymentMethod.Cash, null, owner.Id);
        var clinics = Substitute.For<IClinicRepository>();
        clinics.GetByIdAsync(clinic.Id, Arg.Any<CancellationToken>()).Returns(clinic);
        var billing = Substitute.For<IClinicBillingRepository>();
        billing.GetSaleByIdAsync(sale.Id, Arg.Any<CancellationToken>()).Returns(sale);
        var users = Substitute.For<IUserRepository>();
        users.GetByIdAsync(owner.Id, Arg.Any<CancellationToken>()).Returns(owner);
        var issuer = Substitute.For<IClinicFiscalIssuerRegistry>();
        var gateway = Substitute.For<IClinicFiscalGateway>();
        var handler = new SubmitClinicSaleFiscalCommandHandler(clinics, billing,
            Substitute.For<IClinicFinanceAccessRepository>(), users, issuer, gateway,
            Substitute.For<IAuditLogRepository>(), Substitute.For<IUnitOfWork>());

        var result = await handler.Handle(new SubmitClinicSaleFiscalCommand(clinic.Id, owner.Id, sale.Id), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        await gateway.DidNotReceive().SubmitAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<decimal>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void ProviderReceipt_MeansSubmittedNotAcceptedByHacienda()
    {
        var submission = ClinicFiscalSubmission.Create(Guid.NewGuid(), Guid.NewGuid(), "3101111111", Guid.NewGuid());

        submission.MarkSubmitted("external-123");

        submission.Status.Should().Be(ClinicFiscalSubmissionStatus.SubmittedToProvider);
        submission.ProviderReference.Should().Be("external-123");
        var duplicate = () => submission.MarkSubmitted("external-456");
        duplicate.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public async Task FiscalSale_CannotBeRefundedWithoutCreditNoteFlow()
    {
        var ownerId = Guid.NewGuid();
        var clinic = Clinic.Create(ownerId, "Clinica", "LIC-1", "San Jose", 9.93m, -84.08m, "clinic@test.cr");
        var sale = ClinicSale.Create(clinic.Id, ownerId, null, null, null, "REC-FISCAL");
        sale.AddLine("Servicio", ClinicSaleLineType.Service, 1, 1000m, null, null);
        var payment = sale.RecordPayment(1000m, ClinicPaymentMethod.Cash, null, ownerId);
        var clinics = Substitute.For<IClinicRepository>();
        clinics.GetByIdAsync(clinic.Id, Arg.Any<CancellationToken>()).Returns(clinic);
        var billing = Substitute.For<IClinicBillingRepository>();
        billing.GetSaleByIdAsync(sale.Id, Arg.Any<CancellationToken>()).Returns(sale);
        billing.GetFiscalSubmissionAsync(sale.Id, Arg.Any<CancellationToken>())
            .Returns(ClinicFiscalSubmission.Create(clinic.Id, sale.Id, "3101111111", ownerId));
        var handler = new RecordClinicSaleRefundCommandHandler(clinics, billing,
            Substitute.For<IClinicFinanceAccessRepository>(), Substitute.For<IAuditLogRepository>(), Substitute.For<IUnitOfWork>(),
            Substitute.For<IUserRepository>());

        var result = await handler.Handle(new RecordClinicSaleRefundCommand(clinic.Id, ownerId, sale.Id,
            payment.Id, 1000m, "Error", "REF-FISCAL"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        sale.Refunds.Should().BeEmpty();
    }
}
