using FluentAssertions;
using NSubstitute;
using System.Security.Cryptography;
using System.Text;
using System.Globalization;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Payments.Commands;
using PawTrack.Application.Payments.Interfaces;
using PawTrack.Domain.Payments;

namespace PawTrack.UnitTests.Payments.Commands;

public sealed class PaymentOperationCommandTests
{
    private readonly IPaymentIntentRepository _intents = Substitute.For<IPaymentIntentRepository>();
    private readonly IPaymentOperationRepository _operations = Substitute.For<IPaymentOperationRepository>();
    private readonly IPaymentLedgerRepository _ledger = Substitute.For<IPaymentLedgerRepository>();
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

        var result = await new CapturePaymentCommandHandler(_intents, _operations, _ledger, _gateway, _unitOfWork)
            .Handle(new CapturePaymentCommand(userId, intent.Id, "idem-capture", "corr-capture"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        intent.Status.Should().Be(PaymentIntentStatus.Captured);
        intent.DomainEvents.Should().BeEmpty();
        await _unitOfWork.Received(2).SaveChangesAsync(Arg.Any<CancellationToken>());
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

        var result = await new RefundPaymentCommandHandler(_intents, _operations, _ledger, _gateway, _unitOfWork)
            .Handle(new RefundPaymentCommand(userId, intent.Id, 1000m, "idem-refund", "corr-refund"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        intent.Status.Should().Be(PaymentIntentStatus.Settled);
        intent.RefundedAmountCrc.Should().Be(0m);
        await _unitOfWork.Received(2).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Capture_replay_returns_previous_operation_without_calling_provider()
    {
        var userId = Guid.NewGuid();
        var intent = CreateAuthorizedIntent(userId);
        var requestIdentity = $"capture|{intent.Id}|cs-txn-1|{4990m.ToString("F2", CultureInfo.InvariantCulture)}|CRC";
        var requestHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(requestIdentity))).ToLowerInvariant();
        var operation = PaymentOperation.Create(intent.Id, PaymentOperationType.Capture, "idem-capture", requestHash, "corr");
        operation.MarkSucceeded("capture-1", "{\"success\":true}");
        _intents.GetByIdAsync(intent.Id, Arg.Any<CancellationToken>()).Returns(intent);
        _operations.GetByIdempotencyKeyAsync(PaymentOperationType.Capture, "idem-capture", Arg.Any<CancellationToken>())
            .Returns(operation);

        var result = await new CapturePaymentCommandHandler(_intents, _operations, _ledger, _gateway, _unitOfWork)
            .Handle(new CapturePaymentCommand(userId, intent.Id, "idem-capture", "corr"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.OperationId.Should().Be(operation.Id);
        await _gateway.DidNotReceiveWithAnyArgs().CaptureAsync(default!, default);
    }

    [Fact]
    public async Task Capture_reusing_key_with_different_request_returns_conflict()
    {
        var userId = Guid.NewGuid();
        var intent = CreateAuthorizedIntent(userId);
        var operation = PaymentOperation.Create(intent.Id, PaymentOperationType.Capture, "idem-capture", "different-hash", "corr");
        operation.MarkSucceeded("capture-1", "{\"success\":true}");
        _intents.GetByIdAsync(intent.Id, Arg.Any<CancellationToken>()).Returns(intent);
        _operations.GetByIdempotencyKeyAsync(PaymentOperationType.Capture, "idem-capture", Arg.Any<CancellationToken>())
            .Returns(operation);

        var result = await new CapturePaymentCommandHandler(_intents, _operations, _ledger, _gateway, _unitOfWork)
            .Handle(new CapturePaymentCommand(userId, intent.Id, "idem-capture", "corr"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain("Idempotency-Key ya fue usada con una solicitud diferente.");
    }

    private static PaymentIntent CreateAuthorizedIntent(Guid userId)
    {
        var intent = PaymentIntent.Create(userId, 4990m, "CRC", "PT-OP-1", "idem-op-1", "Subscription");
        intent.MarkPendingCustomerAction();
        intent.MarkAuthorized("cs-txn-1", "auth-1");
        return intent;
    }
}
