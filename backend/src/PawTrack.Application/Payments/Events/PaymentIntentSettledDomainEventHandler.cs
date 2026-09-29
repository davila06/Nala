using MediatR;
using PawTrack.Application.Bounties.Interfaces;
using PawTrack.Application.Bundles;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Subscriptions.Interfaces;
using PawTrack.Domain.Bounties;
using PawTrack.Domain.Payments.Events;
using PawTrack.Domain.Subscriptions;

namespace PawTrack.Application.Payments.Events;

public sealed class PaymentIntentSettledDomainEventHandler(
    ISubscriptionRepository subscriptionRepository,
    IBountyRepository bountyRepository,
    ISender sender,
    IUnitOfWork unitOfWork)
    : INotificationHandler<PaymentIntentSettledDomainEvent>
{
    public async Task Handle(PaymentIntentSettledDomainEvent notification, CancellationToken cancellationToken)
    {
        if (!notification.TargetEntityId.HasValue)
            return;

        if (string.Equals(notification.Purpose, "Subscription", StringComparison.OrdinalIgnoreCase))
        {
            var subscription = await subscriptionRepository.GetByIdAsync(notification.TargetEntityId.Value, cancellationToken);
            if (subscription is not null && subscription.Status == SubscriptionStatus.PendingPayment)
            {
                subscription.Activate(subscription.BillingMonths);
                subscriptionRepository.Update(subscription);
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }

            return;
        }

        if (string.Equals(notification.Purpose, "BundleOrder", StringComparison.OrdinalIgnoreCase))
        {
            await sender.Send(new ConfirmBundlePaymentCommand(notification.TargetEntityId.Value), cancellationToken);
            return;
        }

        if (string.Equals(notification.Purpose, "Bounty", StringComparison.OrdinalIgnoreCase))
        {
            var bounty = await bountyRepository.GetByIdAsync(notification.TargetEntityId.Value, cancellationToken);
            if (bounty is not null && bounty.Status == BountyStatus.PendingDeposit)
            {
                bounty.ConfirmDeposit();
                bountyRepository.Update(bounty);
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }
        }
    }
}
