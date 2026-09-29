using MediatR;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Payments.Interfaces;
using PawTrack.Domain.Common;
using PawTrack.Domain.Payments;

namespace PawTrack.Application.Payments.Commands;

public sealed record PaymentOperationDto(
    Guid PaymentIntentId,
    PaymentIntentStatus Status,
    decimal AmountCrc,
    decimal RefundedAmountCrc,
    string MerchantReference,
    string? ProviderOperationId);

public sealed record CapturePaymentCommand(Guid UserId, Guid PaymentIntentId)
    : IRequest<Result<PaymentOperationDto>>;

public sealed record VoidPaymentCommand(Guid UserId, Guid PaymentIntentId)
    : IRequest<Result<PaymentOperationDto>>;

public sealed record RefundPaymentCommand(Guid UserId, Guid PaymentIntentId, decimal AmountCrc)
    : IRequest<Result<PaymentOperationDto>>;

public sealed class CapturePaymentCommandHandler(
    IPaymentIntentRepository intentRepository,
    IPaymentGatewayService gateway,
    IUnitOfWork unitOfWork)
    : IRequestHandler<CapturePaymentCommand, Result<PaymentOperationDto>>
{
    public async Task<Result<PaymentOperationDto>> Handle(CapturePaymentCommand request, CancellationToken cancellationToken)
    {
        var intent = await intentRepository.GetByIdAsync(request.PaymentIntentId, cancellationToken);
        if (intent is null || intent.UserId != request.UserId)
            return Result.Failure<PaymentOperationDto>("Payment intent no encontrado.");
        if (intent.Status != PaymentIntentStatus.Authorized)
            return Result.Failure<PaymentOperationDto>("El payment intent no está autorizado para captura.");

        var operation = await gateway.CaptureAsync(PaymentOperationCommandMapping.ToRequest(intent), cancellationToken);
        if (!operation.Success)
            return Result.Failure<PaymentOperationDto>(operation.ErrorMessage ?? "No fue posible capturar el pago.");

        intent.MarkCaptured();
        intentRepository.Update(intent);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(PaymentOperationCommandMapping.ToDto(intent, operation.ProviderOperationId));
    }
}

public sealed class VoidPaymentCommandHandler(
    IPaymentIntentRepository intentRepository,
    IPaymentGatewayService gateway,
    IUnitOfWork unitOfWork)
    : IRequestHandler<VoidPaymentCommand, Result<PaymentOperationDto>>
{
    public async Task<Result<PaymentOperationDto>> Handle(VoidPaymentCommand request, CancellationToken cancellationToken)
    {
        var intent = await intentRepository.GetByIdAsync(request.PaymentIntentId, cancellationToken);
        if (intent is null || intent.UserId != request.UserId)
            return Result.Failure<PaymentOperationDto>("Payment intent no encontrado.");
        if (intent.Status != PaymentIntentStatus.Authorized)
            return Result.Failure<PaymentOperationDto>("El payment intent no está autorizado para anulación.");

        var operation = await gateway.VoidAsync(PaymentOperationCommandMapping.ToRequest(intent), cancellationToken);
        if (!operation.Success)
            return Result.Failure<PaymentOperationDto>(operation.ErrorMessage ?? "No fue posible anular el pago.");

        intent.MarkCancelled("Autorización anulada por el usuario.");
        intentRepository.Update(intent);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(PaymentOperationCommandMapping.ToDto(intent, operation.ProviderOperationId));
    }
}

public sealed class RefundPaymentCommandHandler(
    IPaymentIntentRepository intentRepository,
    IPaymentGatewayService gateway,
    IUnitOfWork unitOfWork)
    : IRequestHandler<RefundPaymentCommand, Result<PaymentOperationDto>>
{
    public async Task<Result<PaymentOperationDto>> Handle(RefundPaymentCommand request, CancellationToken cancellationToken)
    {
        var intent = await intentRepository.GetByIdAsync(request.PaymentIntentId, cancellationToken);
        if (intent is null || intent.UserId != request.UserId)
            return Result.Failure<PaymentOperationDto>("Payment intent no encontrado.");
        if (intent.Status is not (PaymentIntentStatus.Settled or PaymentIntentStatus.PartiallyRefunded))
            return Result.Failure<PaymentOperationDto>("El payment intent no está liquidado para reembolso.");
        if (request.AmountCrc <= 0 || request.AmountCrc > intent.CapturedAmountCrc - intent.RefundedAmountCrc)
            return Result.Failure<PaymentOperationDto>("El monto de reembolso no es válido.");

        var operation = await gateway.RefundAsync(
            PaymentOperationCommandMapping.ToRequest(intent) with { AmountCrc = request.AmountCrc },
            cancellationToken);
        if (!operation.Success)
            return Result.Failure<PaymentOperationDto>(operation.ErrorMessage ?? "No fue posible reembolsar el pago.");

        intent.ApplyRefund(request.AmountCrc);
        intentRepository.Update(intent);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(PaymentOperationCommandMapping.ToDto(intent, operation.ProviderOperationId));
    }
}

internal static class PaymentOperationCommandMapping
{
    public static PaymentOperationRequest ToRequest(PaymentIntent intent) =>
        new(intent.GatewayTransactionId!, intent.AmountCrc, intent.MerchantReference, intent.Currency);

    public static PaymentOperationDto ToDto(PaymentIntent intent, string? providerOperationId) =>
        new(intent.Id, intent.Status, intent.AmountCrc, intent.RefundedAmountCrc, intent.MerchantReference, providerOperationId);
}
