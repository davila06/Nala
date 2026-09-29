using FluentValidation;
using MediatR;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Payments.DTOs;
using PawTrack.Application.Payments.Interfaces;
using PawTrack.Domain.Common;
using PawTrack.Domain.Payments;

namespace PawTrack.Application.Payments.Commands.ChargeCard;

public sealed record ChargeCardCommand(
    Guid UserId,
    decimal AmountCrc,
    string Purpose,
    Guid? TargetEntityId = null,
    Guid? PaymentProfileId = null,
    string? TransientToken = null,
    string? CardholderName = null,
    bool SaveProfile = false,
    string? IdempotencyKey = null)
    : IRequest<Result<ChargeCardResultDto>>;

public sealed class ChargeCardCommandValidator : AbstractValidator<ChargeCardCommand>
{
    public ChargeCardCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.AmountCrc).GreaterThan(0).WithMessage("El monto a cobrar debe ser mayor a 0.");
        RuleFor(x => x.Purpose).NotEmpty().MaximumLength(50);
        RuleFor(x => x.IdempotencyKey).NotEmpty().MaximumLength(200);
        RuleFor(x => x)
            .Must(x => x.PaymentProfileId.HasValue || !string.IsNullOrWhiteSpace(x.TransientToken))
            .WithMessage("Debe proporcionar un perfil de pago guardado o un token de tarjeta.");
    }
}

public sealed class ChargeCardCommandHandler(
    IUserPaymentProfileRepository profileRepository,
    IPaymentIntentRepository paymentIntentRepository,
    IPaymentGatewayService paymentGatewayService,
    IUserRepository userRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<ChargeCardCommand, Result<ChargeCardResultDto>>
{
    public async Task<Result<ChargeCardResultDto>> Handle(
        ChargeCardCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.IdempotencyKey))
            return Result.Failure<ChargeCardResultDto>("Idempotency-Key es obligatorio.");

        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
            return Result.Failure<ChargeCardResultDto>("Usuario no encontrado.");

        var existingIntent = await paymentIntentRepository.GetByIdempotencyKeyAsync(
            request.UserId,
            request.IdempotencyKey.Trim(),
            cancellationToken);
        if (existingIntent is not null)
            return Result.Success(ToResult(existingIntent));

        var intent = PaymentIntent.Create(
            request.UserId,
            request.AmountCrc,
            "CRC",
            $"PT-{Guid.CreateVersion7():N}",
            request.IdempotencyKey,
            request.Purpose,
            request.TargetEntityId);
        intent.MarkPendingCustomerAction();
        await paymentIntentRepository.AddAsync(intent, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        string paymentInstrumentToken;
        UserPaymentProfile? usedProfile = null;
        UserPaymentProfile? profileToPersist = null;

        if (request.PaymentProfileId.HasValue)
        {
            usedProfile = await profileRepository.GetByIdAsync(request.PaymentProfileId.Value, cancellationToken);
            if (usedProfile is null || usedProfile.UserId != request.UserId)
            {
                intent.MarkFailed("Perfil de pago no válido o no autorizado.");
                paymentIntentRepository.Update(intent);
                await unitOfWork.SaveChangesAsync(cancellationToken);
                return Result.Failure<ChargeCardResultDto>("Perfil de pago no válido o no autorizado.");
            }

            paymentInstrumentToken = usedProfile.ProviderToken;
        }
        else
        {
            // Transient token from client-side tokenization
            var tokenResult = await paymentGatewayService.TokenizeTransientTokenAsync(
                new TokenizePaymentRequest(request.TransientToken!, request.CardholderName),
                cancellationToken);

            if (!tokenResult.Success)
            {
                intent.MarkFailed(tokenResult.ErrorMessage ?? "Error al tokenizar la tarjeta.");
                paymentIntentRepository.Update(intent);
                await unitOfWork.SaveChangesAsync(cancellationToken);
                return Result.Failure<ChargeCardResultDto>(
                    tokenResult.ErrorMessage ?? "Error al procesar la tarjeta con la pasarela.");
            }

            paymentInstrumentToken = tokenResult.PaymentInstrumentId ?? tokenResult.CustomerProfileId ?? request.TransientToken!;

            if (request.SaveProfile)
            {
                profileToPersist = UserPaymentProfile.CreateCard(
                    request.UserId,
                    paymentInstrumentToken,
                    tokenResult.CardBrand ?? "Card",
                    tokenResult.LastFourDigits ?? "0000",
                    tokenResult.ExpirationMonth,
                    tokenResult.ExpirationYear,
                    request.CardholderName,
                    isDefault: false);
            }
        }

        var chargeResult = await paymentGatewayService.ChargeAsync(
            new ChargePaymentRequest(
                request.AmountCrc,
                paymentInstrumentToken,
                intent.MerchantReference,
                request.Purpose,
                user.Email),
            cancellationToken);

        if (!chargeResult.Success)
        {
            if (string.Equals(chargeResult.ErrorCode, "DECLINED", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(chargeResult.ErrorCode, "INSUFFICIENT_FUNDS", StringComparison.OrdinalIgnoreCase))
                intent.MarkDeclined(chargeResult.ErrorMessage ?? "Transacción declinada.");
            else
                intent.MarkFailed(chargeResult.ErrorMessage ?? "No fue posible autorizar el pago.");

            paymentIntentRepository.Update(intent);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Success(ToResult(intent));
        }

        if (string.IsNullOrWhiteSpace(chargeResult.GatewayTransactionId))
        {
            intent.MarkUnknown("La pasarela no devolvió un identificador de transacción.");
            paymentIntentRepository.Update(intent);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Success(ToResult(intent));
        }

        intent.MarkAuthorized(chargeResult.GatewayTransactionId, chargeResult.AuthorizationCode);
        paymentIntentRepository.Update(intent);
        if (profileToPersist is not null)
        {
            await profileRepository.AddAsync(profileToPersist, cancellationToken);
            usedProfile = profileToPersist;
        }
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(ToResult(intent));
    }

    private static ChargeCardResultDto ToResult(PaymentIntent intent) => new(
        Success: intent.IsSuccessful,
        TransactionReference: intent.MerchantReference,
        AuthorizationCode: intent.AuthorizationCode,
        ErrorMessage: intent.FailureReason,
        PaymentIntentId: intent.Id,
        Status: intent.Status);
}
