using FluentAssertions;
using PawTrack.Domain.Clinics;

namespace PawTrack.UnitTests.Clinics;

public sealed class ClinicBillingDomainTests
{
    [Fact]
    public void Sale_AcceptsLinesDiscountAndPartialPayments()
    {
        var sale = ClinicSale.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "REC-001");
        sale.AddLine("Consulta", ClinicSaleLineType.Service, 1, 15000m, null, null);
        sale.AddLine("Vacuna rabia", ClinicSaleLineType.InventoryItem, 1, 12000m, Guid.NewGuid(), Guid.NewGuid());
        sale.ApplyDiscount(2000m, "Cortesía aprobada", Guid.NewGuid());
        sale.RecordPayment(10000m, ClinicPaymentMethod.Sinpe, "SINPE-1", Guid.NewGuid());

        sale.SubtotalCrc.Should().Be(27000m);
        sale.TotalCrc.Should().Be(25000m);
        sale.PaidCrc.Should().Be(10000m);
        sale.BalanceCrc.Should().Be(15000m);
        sale.Status.Should().Be(ClinicSaleStatus.PartiallyPaid);
    }

    [Fact]
    public void Sale_VoidAfterPayment_RequiresFullRefundWithEvidence()
    {
        var sale = ClinicSale.Create(Guid.NewGuid(), Guid.NewGuid(), null, null, Guid.NewGuid(), "REC-002");
        sale.AddLine("Consulta", ClinicSaleLineType.Service, 1, 10000m, null, null);
        var payment = sale.RecordPayment(10000m, ClinicPaymentMethod.Cash, null, Guid.NewGuid());

        var beforeRefund = () => sale.Void("Error de facturación", Guid.NewGuid());
        beforeRefund.Should().Throw<InvalidOperationException>();
        var withoutEvidence = () => sale.RecordRefund(payment.Id, 10000m, "Error de facturación", "", Guid.NewGuid());
        withoutEvidence.Should().Throw<ArgumentException>();
        sale.RecordRefund(payment.Id, 10000m, "Error de facturación", "REEM-001", Guid.NewGuid());
        var repayRefundedSale = () => sale.RecordPayment(10000m, ClinicPaymentMethod.Cash, null, Guid.NewGuid());
        repayRefundedSale.Should().Throw<InvalidOperationException>();
        var duplicateRefund = () => sale.RecordRefund(payment.Id, 1m, "Duplicada", "REEM-002", Guid.NewGuid());
        duplicateRefund.Should().Throw<InvalidOperationException>();
        sale.Void("Error de facturación", Guid.NewGuid());

        sale.Status.Should().Be(ClinicSaleStatus.Voided);
        sale.VoidReason.Should().Be("Error de facturación");
        sale.PaidCrc.Should().Be(0m);
        sale.Refunds.Should().ContainSingle(refund => refund.PaymentId == payment.Id && refund.AmountCrc == 10000m);
    }

    [Fact]
    public void CashClose_SummarizesPaymentsByMethod()
    {
        var close = ClinicCashClose.Create(
            Guid.NewGuid(),
            DateOnly.FromDateTime(DateTime.UtcNow),
            Guid.NewGuid(),
            [
                new ClinicCashClosePaymentSnapshot(ClinicPaymentMethod.Cash, 10000m),
                new ClinicCashClosePaymentSnapshot(ClinicPaymentMethod.Sinpe, 15000m),
            ]);

        close.TotalCrc.Should().Be(25000m);
        close.PaymentsByMethod.Should().ContainSingle(x => x.Method == ClinicPaymentMethod.Sinpe && x.AmountCrc == 15000m);
    }
}
