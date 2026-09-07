using FluentAssertions;
using PawTrack.Domain.ServiceProviders;

namespace PawTrack.UnitTests.ServiceProviders;

public sealed class ProviderPaymentDomainTests
{
    [Fact]
    public void Create_CapturesBookingTermsAndStartsPending()
    {
        var payment = ProviderPayment.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 25_000m, "SINPE-001", "idem-001");

        payment.Status.Should().Be(ProviderPaymentStatus.Pending);
        payment.AmountCrc.Should().Be(25_000m);
        payment.Currency.Should().Be("CRC");
        payment.PaymentReference.Should().Be("SINPE-001");
        payment.IdempotencyKey.Should().Be("idem-001");
    }

    [Fact]
    public void ReportAndConfirmPayment_TransitionOnlyThroughReportedState()
    {
        var payment = ProviderPayment.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 25_000m, "SINPE-001", "idem-001");

        payment.ReportPayment();
        payment.Confirm("bank-tx-001");

        payment.Status.Should().Be(ProviderPaymentStatus.Confirmed);
        payment.ExternalReference.Should().Be("bank-tx-001");
        payment.ConfirmedAt.Should().NotBeNull();
    }

    [Fact]
    public void Refund_RequiresConfirmedPaymentAndReason()
    {
        var payment = ProviderPayment.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 25_000m, "SINPE-001", "idem-001");
        payment.ReportPayment();
        payment.Confirm("bank-tx-001");

        payment.Refund("Cancelacion dentro de politica");

        payment.Status.Should().Be(ProviderPaymentStatus.Refunded);
        payment.RefundReason.Should().Be("Cancelacion dentro de politica");
    }
}