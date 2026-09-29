using FluentAssertions;
using NSubstitute;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Payments.Commands;
using PawTrack.Application.Payments.Interfaces;
using PawTrack.Domain.Payments;

namespace PawTrack.UnitTests.Payments.Commands;

public sealed class PaymentOperationCommandTests
{
    private readonly IPaymentIntentRepository _intents = Substitute.For<IPaymentIntentRepository>();
    private readonly IPaymentGatewayService _gateway = Substitute.For<IPaymentGatewayService>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    [Fact]
    public async Task Capture_authorized_intent_moves_to_captured_without_settling()
    {
        var userId = Guid.NewGuid();
        var intent = CreateAuthorizedIntent(userId);
        _intents.GetByIdAsync(intent.Id, Arg.Any<CancellationToken>()).Returns(intent);
        _gateway.CaptureAsync(Arg.Any<PaymentOperationRequest>(), Arg.Any<CancellationToken>())
            .Returns(new PaymentOperationResult(true, "capture-1", null, null));

        var result = await new CapturePaymentCommandHandler(_intents, _gateway, _unitOfWork)
            .Handle(new CapturePaymentCommand(userId, intent.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        intent.Status.Should().Be(PaymentIntentStatus.Captured);
        intent.DomainEvents.Should().BeEmpty();
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Refund_provider_failure_keeps_settled_intent_unchanged()
    {
        var userId = Guid.NewGuid();
        var intent = CreateAuthorizedIntent(userId);
        intent.MarkCaptured();
        intent.MarkSettled();
        _intents.GetByIdAsync(intent.Id, Arg.Any<CancellationToken>()).Returns(intent);
        _gateway.RefundAsync(Arg.Any<PaymentOperationRequest>(), Arg.Any<CancellationToken>())
            .Returns(new PaymentOperationResult(false, null, "PROVIDER_ERROR", "Refund rechazado"));

        var result = await new RefundPaymentCommandHandler(_intents, _gateway, _unitOfWork)
            .Handle(new RefundPaymentCommand(userId, intent.Id, 1000m), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        intent.Status.Should().Be(PaymentIntentStatus.Settled);
        intent.RefundedAmountCrc.Should().Be(0m);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private static PaymentIntent CreateAuthorizedIntent(Guid userId)
    {
        var intent = PaymentIntent.Create(userId, 4990m, "CRC", "PT-OP-1", "idem-op-1", "Subscription");
        intent.MarkPendingCustomerAction();
        intent.MarkAuthorized("cs-txn-1", "auth-1");
        return intent;
    }
}
