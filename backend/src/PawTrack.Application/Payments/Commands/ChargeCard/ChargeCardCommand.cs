using FluentValidation;
using MediatR;
using PawTrack.Application.Bounties.Interfaces;
using PawTrack.Application.Bundles.Interfaces;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Payments.DTOs;
using PawTrack.Application.Payments.Interfaces;
using PawTrack.Application.Subscriptions.Interfaces;
using PawTrack.Domain.Common;
using PawTrack.Domain.Bounties;
using PawTrack.Domain.Bundles;
using PawTrack.Domain.Payments;
using PawTrack.Domain.ServiceProviders;
using PawTrack.Domain.Subscriptions;

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
    ISubscriptionRepository subscriptionRepository,
    IBundleOrderRepository bundleOrderRepository,
    IBountyRepository bountyRepository,
    IServiceProviderRepository serviceProviderRepository,
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
        {
            if (!string.Equals(existingIntent.Purpose, request.Purpose, StringComparison.OrdinalIgnoreCase) ||
                existingIntent.TargetEntityId != request.TargetEntityId ||
                existingIntent.AmountCrc != decimal.Round(request.AmountCrc, 2, MidpointRounding.ToEven))
                return Result.Failure<ChargeCardResultDto>("Idempotency-Key ya fue usada con una compra diferente.");

            return Result.Success(ToResult(existingIntent));
        }

        var quote = await ResolvePurchaseQuoteAsync(request, cancellationToken);
        if (quote.AmountCrc is null)
            return Result.Failure<ChargeCardResultDto>(quote.Error ?? "Compra no disponible para pago.");
        if (decimal.Round(request.AmountCrc, 2, MidpointRounding.ToEven) != quote.AmountCrc.Value)
            return Result.Failure<ChargeCardResultDto>("El monto enviado no coincide con el total vigente de la compra.");

        var intent = PaymentIntent.Create(
            request.UserId,
            quote.AmountCrc.Value,
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

        if (quote.ProviderPayment is not null)
        {
            try
            {
                quote.ProviderPayment.BeginCardPayment(intent.Id);
                serviceProviderRepository.UpdatePayment(quote.ProviderPayment);
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (InvalidOperationException exception)
            {
                intent.MarkFailed(exception.Message);
                paymentIntentRepository.Update(intent);
                await unitOfWork.SaveChangesAsync(cancellationToken);
                return Result.Failure<ChargeCardResultDto>("No fue posible reservar este pago para tarjeta.");
            }
        }

        var chargeResult = await paymentGatewayService.ChargeAsync(
            new ChargePaymentRequest(
                intent.AmountCrc,
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
            if (quote.ProviderPayment is not null &&
                !string.Equals(chargeResult.ErrorCode, "NETWORK_ERROR", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(chargeResult.ErrorCode, "TIMEOUT", StringComparison.OrdinalIgnoreCase))
            {
                quote.ProviderPayment.ReturnCardAttemptToPending(
                    intent.Id, chargeResult.ErrorMessage ?? "La pasarela rechazó el pago.");
                serviceProviderRepository.UpdatePayment(quote.ProviderPayment);
            }
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

    private async Task<(decimal? AmountCrc, ProviderPayment? ProviderPayment, string? Error)> ResolvePurchaseQuoteAsync(
        ChargeCardCommand request,
        CancellationToken cancellationToken)
    {
        if (!request.TargetEntityId.HasValue)
            return (null, null, "La compra destino es requerida para procesar el pago.");

        if (string.Equals(request.Purpose, "Subscription", StringComparison.OrdinalIgnoreCase))
        {
            var subscription = await subscriptionRepository.GetByIdAsync(request.TargetEntityId.Value, cancellationToken);
            var isOwner = subscription is not null &&
                          (subscription.UserId == request.UserId || subscription.ClinicOwnerId == request.UserId);
            if (!isOwner || subscription!.Status != SubscriptionStatus.PendingPayment)
                return (null, null, "La suscripción no está disponible para este pago.");
            return (subscription.AmountCrc, null, null);
        }

        if (string.Equals(request.Purpose, "BundleOrder", StringComparison.OrdinalIgnoreCase))
        {
            var order = await bundleOrderRepository.GetByIdAsync(request.TargetEntityId.Value, cancellationToken);
            if (order is null || order.UserId != request.UserId || order.Status != BundleOrderStatus.PendingPayment)
                return (null, null, "El pedido no está disponible para este pago.");
            return (order.AmountCrc, null, null);
        }

        if (string.Equals(request.Purpose, "Bounty", StringComparison.OrdinalIgnoreCase))
        {
            var bounty = await bountyRepository.GetByIdAsync(request.TargetEntityId.Value, cancellationToken);
            if (bounty is null || bounty.OwnerId != request.UserId || bounty.Status != BountyStatus.PendingDeposit)
                return (null, null, "La recompensa no está disponible para este pago.");
            return (bounty.Amount, null, null);
        }

        if (string.Equals(request.Purpose, "ProviderBooking", StringComparison.OrdinalIgnoreCase))
        {
            var booking = await serviceProviderRepository.GetBookingByIdAsync(request.TargetEntityId.Value, cancellationToken);
            var payment = await serviceProviderRepository.GetPaymentByBookingAsync(request.TargetEntityId.Value, cancellationToken);
            if (booking is null || booking.CustomerUserId != request.UserId ||
                booking.Status != ProviderBookingStatus.AwaitingPayment ||
                payment is null || payment.CustomerUserId != request.UserId ||
                payment.Status != ProviderPaymentStatus.Pending || payment.AmountCrc != booking.TotalCrc)
                return (null, null, "La reserva no está disponible para este pago.");
            return (booking.TotalCrc, payment, null);
        }

        return (null, null, "El tipo de compra no admite este flujo de pago.");
    }
}
