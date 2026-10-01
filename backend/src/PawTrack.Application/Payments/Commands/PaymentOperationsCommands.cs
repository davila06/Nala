using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MediatR;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Payments.Interfaces;
using PawTrack.Domain.Common;
using PawTrack.Domain.Payments;

namespace PawTrack.Application.Payments.Commands;

public sealed record PaymentOperationDto(
    Guid PaymentIntentId,
    Guid OperationId,
    PaymentOperationType OperationType,
    PaymentOperationStatus OperationStatus,
    PaymentIntentStatus Status,
    decimal AmountCrc,
    decimal RefundedAmountCrc,
    string MerchantReference,
    string? ProviderOperationId);

public sealed record CapturePaymentCommand(Guid UserId, Guid PaymentIntentId, string? IdempotencyKey = null, string? CorrelationId = null)
    : IRequest<Result<PaymentOperationDto>>;

public sealed record VoidPaymentCommand(Guid UserId, Guid PaymentIntentId, string? IdempotencyKey = null, string? CorrelationId = null)
    : IRequest<Result<PaymentOperationDto>>;

public sealed record RefundPaymentCommand(Guid UserId, Guid PaymentIntentId, decimal AmountCrc, string? IdempotencyKey = null, string? CorrelationId = null)
    : IRequest<Result<PaymentOperationDto>>;

public sealed class CapturePaymentCommandHandler(
    IPaymentIntentRepository intentRepository,
    IPaymentOperationRepository operationRepository,
    IPaymentLedgerRepository ledgerRepository,
    IPaymentGatewayService gateway,
    IUnitOfWork unitOfWork)
    : IRequestHandler<CapturePaymentCommand, Result<PaymentOperationDto>>
{
    public async Task<Result<PaymentOperationDto>> Handle(CapturePaymentCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.IdempotencyKey))
            return Result.Failure<PaymentOperationDto>("Idempotency-Key es obligatorio.");
        var intent = await intentRepository.GetByIdAsync(request.PaymentIntentId, cancellationToken);
        if (intent is null || intent.UserId != request.UserId)
            return Result.Failure<PaymentOperationDto>("Payment intent no encontrado.");
        if (intent.Status != PaymentIntentStatus.Authorized)
            return Result.Failure<PaymentOperationDto>("El payment intent no está autorizado para captura.");

        var operationResult = await PaymentOperationCommandSupport.StartOrReplayAsync(operationRepository, intent.Id, PaymentOperationType.Capture,
            request.IdempotencyKey, request.CorrelationId, $"capture|{intent.Id}|{intent.GatewayTransactionId}|{intent.AmountCrc.ToString("F2", CultureInfo.InvariantCulture)}|{intent.Currency}", unitOfWork, cancellationToken);
        if (operationResult.Existing is not null)
        {
            if (operationResult.HashMismatch)
                return Result.Failure<PaymentOperationDto>("Idempotency-Key ya fue usada con una solicitud diferente.");
            return PaymentOperationCommandSupport.Replay(operationResult.Existing, intent);
        }

        var operation = operationResult.Created!;
        var provider = await gateway.CaptureAsync(PaymentOperationCommandSupport.ToRequest(intent), cancellationToken);
        if (!provider.Success || string.IsNullOrWhiteSpace(provider.ProviderOperationId))
        {
            PaymentOperationCommandSupport.MarkProviderFailure(operation, provider);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Failure<PaymentOperationDto>(provider.ErrorMessage ?? "No fue posible capturar el pago.");
        }

        intent.MarkCaptured();
        operation.MarkSucceeded(provider.ProviderOperationId, PaymentOperationCommandSupport.Normalize(provider));
        intentRepository.Update(intent);
        operationRepository.Update(operation);
        await PaymentOperationCommandSupport.AddLedgerAsync(ledgerRepository, intent, operation, PaymentLedgerEntryType.Debit, null, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(PaymentOperationCommandSupport.ToDto(intent, operation));
    }
}

public sealed class VoidPaymentCommandHandler(
    IPaymentIntentRepository intentRepository,
    IPaymentOperationRepository operationRepository,
    IPaymentGatewayService gateway,
    IUnitOfWork unitOfWork)
    : IRequestHandler<VoidPaymentCommand, Result<PaymentOperationDto>>
{
    public async Task<Result<PaymentOperationDto>> Handle(VoidPaymentCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.IdempotencyKey))
            return Result.Failure<PaymentOperationDto>("Idempotency-Key es obligatorio.");
        var intent = await intentRepository.GetByIdAsync(request.PaymentIntentId, cancellationToken);
        if (intent is null || intent.UserId != request.UserId)
            return Result.Failure<PaymentOperationDto>("Payment intent no encontrado.");
        if (intent.Status != PaymentIntentStatus.Authorized)
            return Result.Failure<PaymentOperationDto>("El payment intent no está autorizado para anulación.");

        var operationResult = await PaymentOperationCommandSupport.StartOrReplayAsync(operationRepository, intent.Id, PaymentOperationType.Void,
            request.IdempotencyKey, request.CorrelationId, $"void|{intent.Id}|{intent.GatewayTransactionId}|{intent.AmountCrc.ToString("F2", CultureInfo.InvariantCulture)}|{intent.Currency}", unitOfWork, cancellationToken);
        if (operationResult.Existing is not null)
        {
            if (operationResult.HashMismatch)
                return Result.Failure<PaymentOperationDto>("Idempotency-Key ya fue usada con una solicitud diferente.");
            return PaymentOperationCommandSupport.Replay(operationResult.Existing, intent);
        }

        var operation = operationResult.Created!;
        var provider = await gateway.VoidAsync(PaymentOperationCommandSupport.ToRequest(intent), cancellationToken);
        if (!provider.Success || string.IsNullOrWhiteSpace(provider.ProviderOperationId))
        {
            PaymentOperationCommandSupport.MarkProviderFailure(operation, provider);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Failure<PaymentOperationDto>(provider.ErrorMessage ?? "No fue posible anular el pago.");
        }

        intent.MarkCancelled("Autorización anulada por el usuario.");
        operation.MarkSucceeded(provider.ProviderOperationId, PaymentOperationCommandSupport.Normalize(provider));
        intentRepository.Update(intent);
        operationRepository.Update(operation);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(PaymentOperationCommandSupport.ToDto(intent, operation));
    }
}

public sealed class RefundPaymentCommandHandler(
    IPaymentIntentRepository intentRepository,
    IPaymentOperationRepository operationRepository,
    IPaymentLedgerRepository ledgerRepository,
    IPaymentGatewayService gateway,
    IUnitOfWork unitOfWork)
    : IRequestHandler<RefundPaymentCommand, Result<PaymentOperationDto>>
{
    public async Task<Result<PaymentOperationDto>> Handle(RefundPaymentCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.IdempotencyKey))
            return Result.Failure<PaymentOperationDto>("Idempotency-Key es obligatorio.");
        var intent = await intentRepository.GetByIdAsync(request.PaymentIntentId, cancellationToken);
        if (intent is null || intent.UserId != request.UserId)
            return Result.Failure<PaymentOperationDto>("Payment intent no encontrado.");
        if (intent.Status is not (PaymentIntentStatus.Settled or PaymentIntentStatus.PartiallyRefunded))
            return Result.Failure<PaymentOperationDto>("El payment intent no está liquidado para reembolso.");
        if (request.AmountCrc <= 0 || request.AmountCrc > intent.CapturedAmountCrc - intent.RefundedAmountCrc)
            return Result.Failure<PaymentOperationDto>("El monto de reembolso no es válido.");

        var operationResult = await PaymentOperationCommandSupport.StartOrReplayAsync(operationRepository, intent.Id, PaymentOperationType.Refund,
            request.IdempotencyKey, request.CorrelationId, $"refund|{intent.Id}|{intent.GatewayTransactionId}|{request.AmountCrc.ToString("F2", CultureInfo.InvariantCulture)}|{intent.Currency}", unitOfWork, cancellationToken);
        if (operationResult.Existing is not null)
        {
            if (operationResult.HashMismatch)
                return Result.Failure<PaymentOperationDto>("Idempotency-Key ya fue usada con una solicitud diferente.");
            return PaymentOperationCommandSupport.Replay(operationResult.Existing, intent);
        }

        var operation = operationResult.Created!;
        var provider = await gateway.RefundAsync(PaymentOperationCommandSupport.ToRequest(intent) with { AmountCrc = request.AmountCrc }, cancellationToken);
        if (!provider.Success || string.IsNullOrWhiteSpace(provider.ProviderOperationId))
        {
            PaymentOperationCommandSupport.MarkProviderFailure(operation, provider);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Failure<PaymentOperationDto>(provider.ErrorMessage ?? "No fue posible reembolsar el pago.");
        }

        intent.ApplyRefund(request.AmountCrc);
        operation.MarkSucceeded(provider.ProviderOperationId, PaymentOperationCommandSupport.Normalize(provider));
        intentRepository.Update(intent);
        operationRepository.Update(operation);
        await PaymentOperationCommandSupport.AddLedgerAsync(ledgerRepository, intent, operation, PaymentLedgerEntryType.Refund, request.AmountCrc, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(PaymentOperationCommandSupport.ToDto(intent, operation));
    }
}

internal static class PaymentOperationCommandSupport
{
    public static async Task<(PaymentOperation? Existing, PaymentOperation? Created, bool HashMismatch)> StartOrReplayAsync(
        IPaymentOperationRepository repository, Guid intentId, PaymentOperationType operationType, string? idempotencyKey,
        string? correlationId, string requestIdentity, IUnitOfWork unitOfWork, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
            throw new InvalidOperationException("Idempotency-Key es obligatorio para operaciones financieras.");

        var key = idempotencyKey.Trim();
        var existing = await repository.GetByIdempotencyKeyAsync(operationType, key, cancellationToken);
        var requestHash = Hash(requestIdentity);
        if (existing is not null) return (existing, null, existing.RequestHash != requestHash);

        var operation = PaymentOperation.Create(intentId, operationType, key, requestHash, correlationId ?? Activity.Current?.Id ?? Guid.CreateVersion7().ToString("N"));
        await repository.AddAsync(operation, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return (null, operation, false);
    }

    public static PaymentOperationRequest ToRequest(PaymentIntent intent) =>
        new(intent.GatewayTransactionId!, intent.AmountCrc, intent.MerchantReference, intent.Currency);

    public static PaymentOperationDto ToDto(PaymentIntent intent, PaymentOperation operation) =>
        new(intent.Id, operation.Id, operation.OperationType, operation.Status, intent.Status, intent.AmountCrc, intent.RefundedAmountCrc,
            intent.MerchantReference, operation.ProviderOperationId);

    public static Result<PaymentOperationDto> Replay(PaymentOperation operation, PaymentIntent intent)
    {
        var dto = ToDto(intent, operation);
        return operation.Status == PaymentOperationStatus.Succeeded
            ? Result.Success(dto)
            : Result.Failure<PaymentOperationDto>(operation.FailureReason ?? "La operación financiera no fue confirmada.");
    }

    public static void MarkProviderFailure(PaymentOperation operation, PaymentOperationResult result)
    {
        var response = Normalize(result);
        if (result.ErrorCode is "NETWORK_ERROR" or "TIMEOUT")
            operation.MarkUnknown(result.ErrorMessage ?? "La respuesta del proveedor es desconocida.", response);
        else
            operation.MarkFailed(result.ErrorMessage ?? "El proveedor rechazó la operación.", response);
    }

    public static async Task AddLedgerAsync(
        IPaymentLedgerRepository repository,
        PaymentIntent intent,
        PaymentOperation operation,
        PaymentLedgerEntryType entryType,
        decimal? operationAmountCrc,
        CancellationToken cancellationToken)
    {
        if (await repository.ExistsByOperationIdAsync(operation.Id, cancellationToken)) return;
        await repository.AddAsync(
            PaymentLedgerEntry.Create(
                intent.Id,
                operation.Id,
                entryType,
                operationAmountCrc ?? intent.CapturedAmountCrc,
                intent.Currency,
                operation.ProviderOperationId ?? intent.MerchantReference,
                operation.CorrelationId),
            cancellationToken);
    }

    public static string Normalize(PaymentOperationResult result) => JsonSerializer.Serialize(new
    {
        result.Success,
        result.ProviderOperationId,
        result.ErrorCode,
        result.ErrorMessage,
    });

    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
}
