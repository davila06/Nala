using FluentAssertions;
using PawTrack.Domain.Payments;

namespace PawTrack.UnitTests.Payments.Domain;

public sealed class UserPaymentProfileDomainTests
{
    [Fact]
    public void CreateCard_WhenValid_SetsPropertiesCorrectly()
    {
        var userId = Guid.NewGuid();
        var profile = UserPaymentProfile.CreateCard(
            userId,
            "tok_test_instrument_123",
            "Visa",
            "4242",
            11,
            2028,
            "JUAN PEREZ",
            isDefault: true);

        profile.Id.Should().NotBeEmpty();
        profile.UserId.Should().Be(userId);
        profile.MethodType.Should().Be(PaymentMethodType.CreditDebitCard);
        profile.ProviderToken.Should().Be("tok_test_instrument_123");
        profile.CardBrand.Should().Be("Visa");
        profile.LastFourDigits.Should().Be("4242");
        profile.ExpirationMonth.Should().Be(11);
        profile.ExpirationYear.Should().Be(2028);
        profile.CardholderName.Should().Be("JUAN PEREZ");
        profile.IsDefault.Should().BeTrue();
        profile.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
        profile.LastUsedAt.Should().BeNull();
    }

    [Fact]
    public void RecordUsage_UpdatesLastUsedAt()
    {
        var profile = UserPaymentProfile.CreateCard(
            Guid.NewGuid(), "tok_123", "Mastercard", "5555", 10, 2029);

        profile.RecordUsage();

        profile.LastUsedAt.Should().NotBeNull();
        profile.LastUsedAt!.Value.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void PaymentTransaction_RecordAndMark_UpdatesLifecycleAccurately()
    {
        var userId = Guid.NewGuid();
        var txn = PaymentTransaction.Record(
            userId,
            2990m,
            "ORDER-REF-1234",
            "Subscription",
            currency: "CRC");

        txn.Status.Should().Be(PaymentTransactionStatus.Pending);
        txn.CompletedAt.Should().BeNull();

        txn.MarkSucceeded("AUTH-999888");

        txn.Status.Should().Be(PaymentTransactionStatus.Succeeded);
        txn.GatewayAuthorizationCode.Should().Be("AUTH-999888");
        txn.CompletedAt.Should().NotBeNull();
    }
}
