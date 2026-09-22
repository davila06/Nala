using MediatR;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Payments.Interfaces;
using PawTrack.Application.Subscriptions.DTOs;
using PawTrack.Application.Subscriptions.Interfaces;
using PawTrack.Application.Subscriptions.Services;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Subscriptions.Commands.ManageSubscriptionAddon;

public sealed record ReplaceSubscriptionAddonCommand(
    Guid AddonId,
    string EntitlementKey,
    decimal Units,
    decimal PriceCrc,
    DateTimeOffset ExpiresAt,
    Guid ActorId = default) : IRequest<Result<ReplaceSubscriptionAddonResult>>;

public sealed record ReplaceSubscriptionAddonResult(
    SubscriptionAddonDto Addon,
    AddonProrationQuote Proration);

public sealed class ReplaceSubscriptionAddonCommandHandler(
    ISubscriptionAddonRepository addonRepository,
    IUnitOfWork unitOfWork,
    ISubscriptionRepository? subscriptionRepository = null,
    IUserPaymentProfileRepository? paymentProfileRepository = null,
    IPaymentTransactionRepository? transactionRepository = null,
    IPaymentGatewayService? gatewayService = null)
    : IRequestHandler<ReplaceSubscriptionAddonCommand, Result<ReplaceSubscriptionAddonResult>>
{
    public async Task<Result<ReplaceSubscriptionAddonResult>> Handle(
        ReplaceSubscriptionAddonCommand request,
        CancellationToken cancellationToken)
    {
        var existing = await addonRepository.GetByIdAsync(request.AddonId, cancellationToken);
        if (existing is null)
            return Result.Failure<ReplaceSubscriptionAddonResult>("Subscription addon not found.");
        if (!existing.IsActive || existing.PriceCrc is null)
            return Result.Failure<ReplaceSubscriptionAddonResult>("El add-on no tiene un precio prorrateable activo.");

        try
        {
            var quote = SubscriptionAddonProration.Quote(
                existing.PriceCrc.Value,
                existing.StartsAt,
                existing.ExpiresAt,
                DateTimeOffset.UtcNow,
                request.PriceCrc);

            if (quote.AmountDueCrc > 0m)
            {
                if (subscriptionRepository is null || paymentProfileRepository is null ||
                    transactionRepository is null || gatewayService is null)
                    return Result.Failure<ReplaceSubscriptionAddonResult>("El pago del reemplazo no está configurado.");

                var subscription = await subscriptionRepository.GetByIdAsync(existing.SubscriptionId, cancellationToken);
                if (subscription?.UserId is null)
                    return Result.Failure<ReplaceSubscriptionAddonResult>("El add-on no tiene un propietario con pago electrónico.");
                var profile = await paymentProfileRepository.GetDefaultByUserIdAsync(subscription.UserId.Value, cancellationToken);
                if (profile is null)
                    return Result.Failure<ReplaceSubscriptionAddonResult>("No existe una tarjeta predeterminada para cobrar el reemplazo.");

                var transaction = Domain.Payments.PaymentTransaction.Record(
                    subscription.UserId.Value, quote.AmountDueCrc,
                    $"ADDON-{Guid.NewGuid():N}"[..16].ToUpperInvariant(),
                    "SubscriptionAddonReplacement", profile.Id, existing.SubscriptionId);
                transaction.ApplyProration(quote.NewTermPriceCrc, quote.UnusedCreditCrc);
                await transactionRepository.AddAsync(transaction, cancellationToken);
                await unitOfWork.SaveChangesAsync(cancellationToken);

                var charge = await gatewayService.ChargeAsync(new ChargePaymentRequest(
                    quote.AmountDueCrc, profile.ProviderToken, transaction.TransactionReference,
                    "SubscriptionAddonReplacement"), cancellationToken);
                if (!charge.Success)
                {
                    transaction.MarkFailed(charge.ErrorMessage ?? "El cobro del reemplazo fue rechazado.");
                    transactionRepository.Update(transaction);
                    await unitOfWork.SaveChangesAsync(cancellationToken);
                    return Result.Failure<ReplaceSubscriptionAddonResult>("El cobro del reemplazo fue rechazado.");
                }
                transaction.MarkSucceeded(charge.AuthorizationCode);
                transactionRepository.Update(transaction);
                profile.RecordUsage();
                paymentProfileRepository.Update(profile);
            }

            existing.Replace();
            addonRepository.Update(existing);
            var replacement = Domain.Subscriptions.SubscriptionAddon.Create(
                existing.SubscriptionId, request.EntitlementKey, request.Units,
                DateTimeOffset.UtcNow, request.ExpiresAt, request.PriceCrc);
            await addonRepository.AddAsync(replacement, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Success(new ReplaceSubscriptionAddonResult(
                SubscriptionAddonDto.FromDomain(replacement), quote));
        }
        catch (ArgumentException exception)
        {
            return Result.Failure<ReplaceSubscriptionAddonResult>(exception.Message);
        }
    }
}
