using MediatR;
using PawTrack.Application.Bounties.Interfaces;
using PawTrack.Application.Bundles;
using PawTrack.Application.Bundles.Interfaces;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Payments.Interfaces;
using PawTrack.Application.ServiceProviders;
using PawTrack.Application.Subscriptions.Interfaces;
using PawTrack.Domain.Bounties;
using PawTrack.Domain.Bundles;
using PawTrack.Domain.Payments;
using PawTrack.Domain.Payments.Events;
using PawTrack.Domain.ServiceProviders;
using PawTrack.Domain.Subscriptions;

namespace PawTrack.Application.Payments.Events;

public sealed class PaymentIntentSettledDomainEventHandler(
    ISubscriptionRepository subscriptionRepository,
    IBountyRepository bountyRepository,
    IBundleOrderRepository bundleRepository,
    IServiceProviderRepository providerRepository,
    IPaymentLedgerRepository ledgerRepository,
    ISender sender,
    IUnitOfWork unitOfWork)
    : INotificationHandler<PaymentIntentSettledDomainEvent>
{
    public async Task Handle(PaymentIntentSettledDomainEvent notification, CancellationToken cancellationToken)
    {
        var ledgerAdded = false;
        if (notification.PaymentOperationId.HasValue &&
            !await ledgerRepository.ExistsByOperationIdAsync(notification.PaymentOperationId.Value, cancellationToken))
        {
            var reference = notification.GatewayTransactionId ?? notification.PaymentIntentId.ToString("N");
            var correlationId = notification.CorrelationId ?? notification.PaymentIntentId.ToString("N");
            await ledgerRepository.AddAsync(PaymentLedgerEntry.Create(
                notification.PaymentIntentId,
                notification.PaymentOperationId.Value,
                PaymentLedgerEntryType.Debit,
                notification.AmountCrc,
                "CRC",
                reference,
                correlationId), cancellationToken);
            ledgerAdded = true;
        }

        if (!notification.TargetEntityId.HasValue)
        {
            if (ledgerAdded) await unitOfWork.SaveChangesAsync(cancellationToken);
            return;
        }

        if (string.Equals(notification.Purpose, "Subscription", StringComparison.OrdinalIgnoreCase))
        {
            var subscription = await subscriptionRepository.GetByIdAsync(notification.TargetEntityId.Value, cancellationToken);
            var isOwner = subscription is not null &&
                          (subscription.UserId == notification.UserId || subscription.ClinicOwnerId == notification.UserId);
            var amountMatches = subscription is not null && subscription.AmountCrc == notification.AmountCrc;
            if (subscription is not null &&
                subscription.Status == SubscriptionStatus.PendingPayment &&
                isOwner &&
                amountMatches)
            {
                subscription.Activate(subscription.BillingMonths);
                subscriptionRepository.Update(subscription);
                ledgerAdded = true;
            }

            if (ledgerAdded) await unitOfWork.SaveChangesAsync(cancellationToken);

            return;
        }

        if (string.Equals(notification.Purpose, "BundleOrder", StringComparison.OrdinalIgnoreCase))
        {
            var bundle = await bundleRepository.GetByIdAsync(notification.TargetEntityId.Value, cancellationToken);
            if (bundle is not null && bundle.UserId == notification.UserId &&
                bundle.Status == BundleOrderStatus.PendingPayment && bundle.AmountCrc == notification.AmountCrc)
            {
                await sender.Send(new ConfirmBundlePaymentCommand(bundle.Id), cancellationToken);
                ledgerAdded = true;
            }
            if (ledgerAdded) await unitOfWork.SaveChangesAsync(cancellationToken);
            return;
        }

        if (string.Equals(notification.Purpose, "Bounty", StringComparison.OrdinalIgnoreCase))
        {
            var bounty = await bountyRepository.GetByIdAsync(notification.TargetEntityId.Value, cancellationToken);
            if (bounty is not null && bounty.OwnerId == notification.UserId &&
                bounty.Status == BountyStatus.PendingDeposit && bounty.Amount == notification.AmountCrc)
            {
                bounty.ConfirmDeposit();
                bountyRepository.Update(bounty);
                ledgerAdded = true;
            }
            if (ledgerAdded) await unitOfWork.SaveChangesAsync(cancellationToken);
            return;
        }

        if (string.Equals(notification.Purpose, "ProviderBooking", StringComparison.OrdinalIgnoreCase))
        {
            var booking = await providerRepository.GetBookingByIdAsync(notification.TargetEntityId.Value, cancellationToken);
            var payment = await providerRepository.GetPaymentByBookingAsync(notification.TargetEntityId.Value, cancellationToken);
            var purchaseMatches = booking is not null && payment is not null &&
                                  booking.CustomerUserId == notification.UserId &&
                                  booking.TotalCrc == notification.AmountCrc &&
                                  payment.CustomerUserId == notification.UserId &&
                                  payment.AmountCrc == notification.AmountCrc &&
                                  payment.PaymentIntentId == notification.PaymentIntentId;
            if (purchaseMatches && payment!.Status == ProviderPaymentStatus.CardPending &&
                booking!.Status == ProviderBookingStatus.AwaitingPayment &&
                !string.IsNullOrWhiteSpace(notification.GatewayTransactionId))
            {
                payment.ConfirmCardPayment(notification.PaymentIntentId, notification.GatewayTransactionId);
                booking.Confirm();
                providerRepository.UpdatePayment(payment);
                providerRepository.UpdateBooking(booking);
                ledgerAdded = true;
            }
            else if (purchaseMatches && payment!.Status == ProviderPaymentStatus.Confirmed &&
                     booking!.Status == ProviderBookingStatus.Confirmed &&
                     payment.ExternalReference == notification.GatewayTransactionId)
            {
                // Webhook replay: the settled payment and booking already match this transaction.
            }
            if (ledgerAdded) await unitOfWork.SaveChangesAsync(cancellationToken);
            return;
        }

        if (ledgerAdded) await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
