using FluentAssertions;
using MediatR;
using NSubstitute;
using PawTrack.Application.Bounties.Interfaces;
using PawTrack.Application.Bundles.Interfaces;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Payments.Events;
using PawTrack.Application.Payments.Interfaces;
using PawTrack.Application.ServiceProviders;
using PawTrack.Application.Subscriptions.Interfaces;
using PawTrack.Domain.Payments.Events;
using PawTrack.Domain.Payments;
using PawTrack.Domain.ServiceProviders;
using PawTrack.Domain.Subscriptions;

namespace PawTrack.UnitTests.Payments.Events;

public sealed class PaymentIntentSettledDomainEventHandlerTests
{
    private readonly ISubscriptionRepository _subscriptions = Substitute.For<ISubscriptionRepository>();
    private readonly IBountyRepository _bounties = Substitute.For<IBountyRepository>();
    private readonly IBundleOrderRepository _bundles = Substitute.For<IBundleOrderRepository>();
    private readonly IServiceProviderRepository _providers = Substitute.For<IServiceProviderRepository>();
    private readonly IPaymentLedgerRepository _ledger = Substitute.For<IPaymentLedgerRepository>();
    private readonly ISender _sender = Substitute.For<ISender>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    [Fact]
    public async Task Settled_subscription_event_activates_pending_subscription_once()
    {
        var userId = Guid.NewGuid();
        var subscription = Subscription.CreateForUser(userId, SubscriptionTier.UserPlus, "REF-1", 2990m);
        _subscriptions.GetByIdAsync(subscription.Id, Arg.Any<CancellationToken>()).Returns(subscription);
        var handler = CreateHandler();
        var notification = new PaymentIntentSettledDomainEvent(
            Guid.NewGuid(), userId, "Subscription", subscription.Id, 2990m);

        await handler.Handle(notification, CancellationToken.None);
        await handler.Handle(notification, CancellationToken.None);

        subscription.Status.Should().Be(SubscriptionStatus.Active);
        _subscriptions.Received(1).Update(subscription);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Settled_subscription_event_with_different_owner_does_not_activate_subscription()
    {
        var ownerId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var subscription = Subscription.CreateForUser(ownerId, SubscriptionTier.UserPlus, "REF-2", 2990m);
        _subscriptions.GetByIdAsync(subscription.Id, Arg.Any<CancellationToken>()).Returns(subscription);
        var handler = CreateHandler();
        var notification = new PaymentIntentSettledDomainEvent(
            Guid.NewGuid(), otherUserId, "Subscription", subscription.Id, 2990m);

        await handler.Handle(notification, CancellationToken.None);

        subscription.Status.Should().Be(SubscriptionStatus.PendingPayment);
        _subscriptions.DidNotReceive().Update(subscription);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Settled_subscription_event_with_different_amount_does_not_activate_subscription()
    {
        var userId = Guid.NewGuid();
        var subscription = Subscription.CreateForUser(userId, SubscriptionTier.UserPlus, "REF-3", 2990m);
        _subscriptions.GetByIdAsync(subscription.Id, Arg.Any<CancellationToken>()).Returns(subscription);
        var handler = CreateHandler();
        var notification = new PaymentIntentSettledDomainEvent(
            Guid.NewGuid(), userId, "Subscription", subscription.Id, 1m);

        await handler.Handle(notification, CancellationToken.None);

        subscription.Status.Should().Be(SubscriptionStatus.PendingPayment);
        _subscriptions.DidNotReceive().Update(subscription);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Settled_provider_booking_confirms_linked_payment_and_booking_once()
    {
        var ownerId = Guid.NewGuid();
        var intentId = Guid.NewGuid();
        var providerId = Guid.NewGuid();
        var gatewayTransactionId = "cybersource-booking-1";
        var booking = ProviderBooking.Request(providerId, Guid.NewGuid(), ownerId, Guid.NewGuid(), "Consulta",
            DateTimeOffset.UtcNow.AddDays(2), 60, 20_000m, 1, null, taxCrc: 2600m, platformFeeCrc: 500m);
        booking.MarkAwaitingPayment();
        var payment = ProviderPayment.Create(booking.Id, ownerId, providerId, booking.TotalCrc, "CARD-BOOKING", "idem-booking");
        payment.BeginCardPayment(intentId);
        _providers.GetBookingByIdAsync(booking.Id, Arg.Any<CancellationToken>()).Returns(booking);
        _providers.GetPaymentByBookingAsync(booking.Id, Arg.Any<CancellationToken>()).Returns(payment);
        var webhookOperationId = Guid.NewGuid();
        var ledgerEntryWritten = false;
        _ledger.ExistsByOperationIdAsync(webhookOperationId, Arg.Any<CancellationToken>())
            .Returns(_ => ledgerEntryWritten);
        _ledger.AddAsync(Arg.Any<PaymentLedgerEntry>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                ledgerEntryWritten = true;
                return Task.CompletedTask;
            });

        var handler = new PaymentIntentSettledDomainEventHandler(
            _subscriptions, _bounties, _bundles, _providers, _ledger, _sender, _unitOfWork);
        var notification = new PaymentIntentSettledDomainEvent(
            intentId, ownerId, "ProviderBooking", booking.Id, booking.TotalCrc,
            gatewayTransactionId, webhookOperationId, "corr-booking-1");

        await handler.Handle(notification, CancellationToken.None);
        await handler.Handle(notification, CancellationToken.None);

        booking.Status.Should().Be(ProviderBookingStatus.Confirmed);
        payment.Status.Should().Be(ProviderPaymentStatus.Confirmed);
        payment.PaymentIntentId.Should().Be(intentId);
        _providers.Received(1).UpdateBooking(booking);
        _providers.Received(1).UpdatePayment(payment);
        await _ledger.Received(1).AddAsync(
            Arg.Is<PaymentLedgerEntry>(entry => entry.PaymentOperationId == webhookOperationId),
            Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private PaymentIntentSettledDomainEventHandler CreateHandler() =>
        new(_subscriptions, _bounties, _bundles, _providers, _ledger, _sender, _unitOfWork);
}
