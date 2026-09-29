using FluentAssertions;
using PawTrack.Domain.Payments;

namespace PawTrack.UnitTests.Payments.Domain;

public sealed class PaymentIntentDomainTests
{
    [Fact]
    public void Create_starts_in_created_state_with_stable_business_identity()
    {
        var userId = Guid.NewGuid();
        var intent = PaymentIntent.Create(
            userId,
            4990m,
            "CRC",
            "PT-ORDER-001",
            "idem-001",
            "Subscription",
            Guid.NewGuid());

        intent.Status.Should().Be(PaymentIntentStatus.Created);
        intent.UserId.Should().Be(userId);
        intent.AmountCrc.Should().Be(4990m);
        intent.Currency.Should().Be("CRC");
        intent.MerchantReference.Should().Be("PT-ORDER-001");
        intent.IdempotencyKey.Should().Be("idem-001");
        intent.CapturedAmountCrc.Should().Be(0m);
        intent.RefundedAmountCrc.Should().Be(0m);
    }

    [Fact]
    public void Authorize_and_capture_follow_the_payment_lifecycle()
    {
        var intent = CreateIntent();

        intent.MarkPendingCustomerAction();
        intent.MarkAuthorized("cs-txn-1", "auth-1");
        intent.MarkCaptured();
        intent.MarkSettled();

        intent.Status.Should().Be(PaymentIntentStatus.Settled);
        intent.DomainEvents.Should().ContainSingle();
        intent.GatewayTransactionId.Should().Be("cs-txn-1");
        intent.AuthorizationCode.Should().Be("auth-1");
        intent.CapturedAmountCrc.Should().Be(intent.AmountCrc);
    }

    [Fact]
    public void Cannot_capture_before_authorization()
    {
        var act = () => CreateIntent().MarkCaptured();

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Refunds_track_partial_and_full_refunds_without_over_refunding()
    {
        var intent = CreateIntent();
        intent.MarkPendingCustomerAction();
        intent.MarkAuthorized("cs-txn-1", "auth-1");
        intent.MarkCaptured();
        intent.MarkSettled();

        intent.ApplyRefund(1000m);
        intent.Status.Should().Be(PaymentIntentStatus.PartiallyRefunded);
        intent.RefundedAmountCrc.Should().Be(1000m);

        intent.ApplyRefund(3990m);
        intent.Status.Should().Be(PaymentIntentStatus.Refunded);
        intent.RefundedAmountCrc.Should().Be(4990m);

        var act = () => intent.ApplyRefund(1m);
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Provider_uncertainty_is_not_success()
    {
        var intent = CreateIntent();

        intent.MarkUnknown("provider timeout");

        intent.Status.Should().Be(PaymentIntentStatus.Unknown);
        intent.IsSuccessful.Should().BeFalse();
        intent.FailureReason.Should().Be("provider timeout");
    }

    private static PaymentIntent CreateIntent() => PaymentIntent.Create(
        Guid.NewGuid(),
        4990m,
        "CRC",
        "PT-ORDER-001",
        "idem-001",
        "Subscription",
        Guid.NewGuid());
}
