using FluentAssertions;
using NSubstitute;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Payments.Commands;
using PawTrack.Application.Payments.Interfaces;

namespace PawTrack.UnitTests.Payments.Commands;

public sealed class RecordPaymentReconciliationCommandTests
{
    [Fact]
    public async Task Records_a_succeeded_reconciliation_operation_idempotently()
    {
        var operations = Substitute.For<IPaymentOperationRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var command = new RecordPaymentReconciliationCommand(
            "settlement-2026-09-30",
            "request-hash",
            "corr-reconciliation",
            true,
            "{\"matched\":10,\"differences\":0}");
        var handler = new RecordPaymentReconciliationCommandHandler(operations, unitOfWork);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await operations.Received(1).AddAsync(Arg.Is<PawTrack.Domain.Payments.PaymentOperation>(operation =>
            operation.OperationType == PawTrack.Domain.Payments.PaymentOperationType.Reconciliation &&
            operation.Status == PawTrack.Domain.Payments.PaymentOperationStatus.Succeeded), Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
