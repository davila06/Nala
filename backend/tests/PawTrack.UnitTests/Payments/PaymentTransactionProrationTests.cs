using FluentAssertions;
using PawTrack.Domain.Payments;

namespace PawTrack.UnitTests.Payments;

public sealed class PaymentTransactionProrationTests
{
    [Fact]
    public void ApplyProration_persists_gross_credit_and_net_amount()
    {
        var transaction = PaymentTransaction.Record(Guid.NewGuid(), 800m, "REF-1", "Subscription");

        transaction.ApplyProration(1000m, 200m);

        transaction.GrossAmountCrc.Should().Be(1000m);
        transaction.ProrationCreditCrc.Should().Be(200m);
        transaction.AmountCrc.Should().Be(800m);
    }
}
