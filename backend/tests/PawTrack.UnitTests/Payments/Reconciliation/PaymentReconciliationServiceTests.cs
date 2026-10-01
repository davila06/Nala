using FluentAssertions;
using NSubstitute;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Payments.Interfaces;
using PawTrack.Application.Payments.Reconciliation;
using PawTrack.Domain.Payments;

namespace PawTrack.UnitTests.Payments.Reconciliation;

public sealed class PaymentReconciliationServiceTests
{
    [Fact]
    public async Task Detects_authorized_without_settlement_and_settlement_without_internal_fulfillment()
    {
        var intents = Substitute.For<IPaymentIntentRepository>();
        var operations = Substitute.For<IPaymentOperationRepository>();
        var ledger = Substitute.For<IPaymentLedgerRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var authorized = CreateIntent("PT-AUTH", PaymentIntentStatus.Authorized);
        var settled = CreateIntent("PT-SETTLED", PaymentIntentStatus.Authorized);
        intents.GetByStatusSinceAsync(PaymentIntentStatus.Authorized, Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(new[] { authorized });
        intents.GetByMerchantReferenceAsync("PT-SETTLED", Arg.Any<CancellationToken>()).Returns(settled);
        ledger.ExistsByIntentAndEntryTypeAsync(settled.Id, PaymentLedgerEntryType.Debit, Arg.Any<CancellationToken>())
            .Returns(false);

        var service = new PaymentReconciliationService(intents, operations, ledger, unitOfWork);
        var result = await service.ReconcileAsync(
            new[] { new PaymentSettlementSnapshot("PT-SETTLED", "provider-1", 4990m, "CRC", "SETTLED") },
            DateTimeOffset.UtcNow.AddDays(-1),
            "corr-reconciliation",
            CancellationToken.None);

        result.Issues.Select(issue => issue.Code).Should().Contain(new[]
        {
            PaymentReconciliationIssueCode.AuthorizedWithoutSettlement,
            PaymentReconciliationIssueCode.SettlementWithoutFulfillment,
        });
        await operations.Received(1).AddAsync(Arg.Is<PaymentOperation>(operation =>
            operation.OperationType == PaymentOperationType.Reconciliation &&
            operation.Status == PaymentOperationStatus.Failed), Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private static PaymentIntent CreateIntent(string reference, PaymentIntentStatus targetStatus)
    {
        var intent = PaymentIntent.Create(Guid.NewGuid(), 4990m, "CRC", reference, Guid.NewGuid().ToString("N"), "Subscription");
        intent.MarkPendingCustomerAction();
        intent.MarkAuthorized($"provider-{reference}", "auth");
        if (targetStatus == PaymentIntentStatus.Captured)
            intent.MarkCaptured();
        if (targetStatus == PaymentIntentStatus.Settled)
        {
            intent.MarkCaptured();
            intent.MarkSettled();
        }
        return intent;
    }
}
