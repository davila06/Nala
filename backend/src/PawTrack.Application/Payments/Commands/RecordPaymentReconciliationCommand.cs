using MediatR;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Payments.Interfaces;
using PawTrack.Domain.Common;
using PawTrack.Domain.Payments;

namespace PawTrack.Application.Payments.Commands;

public sealed record RecordPaymentReconciliationCommand(
    string ReconciliationKey,
    string RequestHash,
    string CorrelationId,
    bool Succeeded,
    string ResponseJson) : IRequest<Result<Guid>>;

public sealed class RecordPaymentReconciliationCommandHandler(
    IPaymentOperationRepository operationRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<RecordPaymentReconciliationCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(
        RecordPaymentReconciliationCommand request,
        CancellationToken cancellationToken)
    {
        var existing = await operationRepository.GetByIdempotencyKeyAsync(
            PaymentOperationType.Reconciliation,
            request.ReconciliationKey,
            cancellationToken);
        if (existing is not null)
            return Result.Success(existing.Id);

        var operation = PaymentOperation.Create(
            paymentIntentId: null,
            PaymentOperationType.Reconciliation,
            request.ReconciliationKey,
            request.RequestHash,
            request.CorrelationId);

        if (request.Succeeded)
            operation.MarkSucceeded(null, request.ResponseJson);
        else
            operation.MarkFailed("La conciliación detectó diferencias.", request.ResponseJson);

        await operationRepository.AddAsync(operation, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(operation.Id);
    }
}
