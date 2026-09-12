using FluentValidation;
using MediatR;
using PawTrack.Application.Bounties.Commands.ConfirmBountyDeposit;
using PawTrack.Application.Bounties.Interfaces;
using PawTrack.Application.Bundles;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Payments.DTOs;
using PawTrack.Application.Payments.Interfaces;
using PawTrack.Application.Subscriptions.Interfaces;
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
    bool SaveProfile = false)
    : IRequest<Result<ChargeCardResultDto>>;

public sealed class ChargeCardCommandValidator : AbstractValidator<ChargeCardCommand>
{
    public ChargeCardCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.AmountCrc).GreaterThan(0).WithMessage("El monto a cobrar debe ser mayor a 0.");
        RuleFor(x => x.Purpose).NotEmpty().MaximumLength(50);
        RuleFor(x => x)
            .Must(x => x.PaymentProfileId.HasValue || !string.IsNullOrWhiteSpace(x.TransientToken))
            .WithMessage("Debe proporcionar un perfil de pago guardado o un token de tarjeta.");
    }
}

public sealed class ChargeCardCommandHandler(
    IUserPaymentProfileRepository profileRepository,
    IPaymentTransactionRepository transactionRepository,
    IPaymentGatewayService paymentGatewayService,
    ISubscriptionRepository subscriptionRepository,
    IBountyRepository bountyRepository,
    IUserRepository userRepository,
    IElectronicBillingService billingService,
    ISender sender,
    IUnitOfWork unitOfWork)
    : IRequestHandler<ChargeCardCommand, Result<ChargeCardResultDto>>
{
    public async Task<Result<ChargeCardResultDto>> Handle(
        ChargeCardCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
            return Result.Failure<ChargeCardResultDto>("Usuario no encontrado.");

        string paymentInstrumentToken;
        UserPaymentProfile? usedProfile = null;

        if (request.PaymentProfileId.HasValue)
        {
            usedProfile = await profileRepository.GetByIdAsync(request.PaymentProfileId.Value, cancellationToken);
            if (usedProfile is null || usedProfile.UserId != request.UserId)
                return Result.Failure<ChargeCardResultDto>("Perfil de pago no válido o no autorizado.");

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
                return Result.Failure<ChargeCardResultDto>(
                    tokenResult.ErrorMessage ?? "Error al procesar la tarjeta con la pasarela.");
            }

            paymentInstrumentToken = tokenResult.PaymentInstrumentId ?? tokenResult.CustomerProfileId ?? request.TransientToken!;

            if (request.SaveProfile)
            {
                var newProfile = UserPaymentProfile.CreateCard(
                    request.UserId,
                    paymentInstrumentToken,
                    tokenResult.CardBrand ?? "Card",
                    tokenResult.LastFourDigits ?? "0000",
                    tokenResult.ExpirationMonth,
                    tokenResult.ExpirationYear,
                    request.CardholderName,
                    isDefault: false);

                await profileRepository.AddAsync(newProfile, cancellationToken);
                usedProfile = newProfile;
            }
        }

        var orderRef = $"PT-{Guid.NewGuid():N}"[..16].ToUpperInvariant();

        var transaction = PaymentTransaction.Record(
            request.UserId,
            request.AmountCrc,
            orderRef,
            request.Purpose,
            usedProfile?.Id,
            request.TargetEntityId);

        await transactionRepository.AddAsync(transaction, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Call payment gateway for authorization & capture
        var chargeResult = await paymentGatewayService.ChargeAsync(
            new ChargePaymentRequest(
                request.AmountCrc,
                paymentInstrumentToken,
                orderRef,
                request.Purpose,
                user.Email),
            cancellationToken);

        if (!chargeResult.Success)
        {
            transaction.MarkFailed(chargeResult.ErrorMessage ?? "Transacción declinada.");
            transactionRepository.Update(transaction);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success(new ChargeCardResultDto(
                Success: false,
                TransactionReference: orderRef,
                AuthorizationCode: null,
                ErrorMessage: chargeResult.ErrorMessage ?? "La transacción fue declinada por el banco emisor."));
        }

        transaction.MarkSucceeded(chargeResult.AuthorizationCode);
        transactionRepository.Update(transaction);
        if (usedProfile is not null)
        {
            usedProfile.RecordUsage();
            profileRepository.Update(usedProfile);
        }
        await unitOfWork.SaveChangesAsync(cancellationToken);

        Guid? activatedSubId = null;
        Guid? confirmedBundleId = null;
        Guid? confirmedBountyId = null;

        // Fulfill business outcome based on purpose
        if (string.Equals(request.Purpose, "Subscription", StringComparison.OrdinalIgnoreCase)
            && request.TargetEntityId.HasValue)
        {
            var sub = await subscriptionRepository.GetByIdAsync(request.TargetEntityId.Value, cancellationToken);
            if (sub is not null && sub.Status == Domain.Subscriptions.SubscriptionStatus.PendingPayment)
            {
                sub.Activate(sub.BillingMonths);
                subscriptionRepository.Update(sub);
                await unitOfWork.SaveChangesAsync(cancellationToken);
                activatedSubId = sub.Id;
            }
        }
        else if (string.Equals(request.Purpose, "BundleOrder", StringComparison.OrdinalIgnoreCase)
            && request.TargetEntityId.HasValue)
        {
            var confirmResult = await sender.Send(new ConfirmBundlePaymentCommand(request.TargetEntityId.Value), cancellationToken);
            if (confirmResult.IsSuccess)
            {
                confirmedBundleId = request.TargetEntityId.Value;
            }
        }
        else if (string.Equals(request.Purpose, "Bounty", StringComparison.OrdinalIgnoreCase)
            && request.TargetEntityId.HasValue)
        {
            var bounty = await bountyRepository.GetByIdAsync(request.TargetEntityId.Value, cancellationToken);
            if (bounty is not null && bounty.Status == Domain.Bounties.BountyStatus.PendingDeposit)
            {
                bounty.ConfirmDeposit();
                bountyRepository.Update(bounty);
                await unitOfWork.SaveChangesAsync(cancellationToken);
                confirmedBountyId = bounty.Id;
            }
        }

        // Automatic Electronic Invoicing DGT Costa Rica
        try
        {
            var cabys = string.Equals(request.Purpose, "BundleOrder", StringComparison.OrdinalIgnoreCase)
                ? CabysCatalog.GpsHardwareTrackerCabys
                : CabysCatalog.SoftwareSubscriptionCabys;

            var desc = string.Equals(request.Purpose, "BundleOrder", StringComparison.OrdinalIgnoreCase)
                ? "Dispositivo y Collar GPS Inteligente PawTrack"
                : "Suscripción PawTrack SaaS Protección Animal";

            await billingService.EmitInvoiceForTransactionAsync(
                new EmitInvoiceRequest(
                    UserId: request.UserId,
                    TotalAmountCrc: request.AmountCrc,
                    Description: desc,
                    CodigoCabys: cabys,
                    PaymentMethodCode: "02",
                    TransactionId: transaction.Id),
                cancellationToken);
        }
        catch
        {
            // Non-blocking: background invoice generation should not reverse authorized card payment
        }

        return Result.Success(new ChargeCardResultDto(
            Success: true,
            TransactionReference: orderRef,
            AuthorizationCode: chargeResult.AuthorizationCode,
            ErrorMessage: null,
            ActivatedSubscriptionId: activatedSubId,
            ConfirmedBundleOrderId: confirmedBundleId,
            ConfirmedBountyId: confirmedBountyId));
    }
}
