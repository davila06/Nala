using FluentAssertions;
using MediatR;
using NSubstitute;
using PawTrack.Application.Bounties.Interfaces;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Payments.Events;
using PawTrack.Application.Subscriptions.Interfaces;
using PawTrack.Domain.Payments.Events;
using PawTrack.Domain.Subscriptions;

namespace PawTrack.UnitTests.Payments.Events;

public sealed class PaymentIntentSettledDomainEventHandlerTests
{
    private readonly ISubscriptionRepository _subscriptions = Substitute.For<ISubscriptionRepository>();
    private readonly IBountyRepository _bounties = Substitute.For<IBountyRepository>();
    private readonly ISender _sender = Substitute.For<ISender>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    [Fact]
    public async Task Settled_subscription_event_activates_pending_subscription_once()
    {
        var userId = Guid.NewGuid();
        var subscription = Subscription.CreateForUser(userId, SubscriptionTier.UserPlus, "REF-1", 2990m);
        _subscriptions.GetByIdAsync(subscription.Id, Arg.Any<CancellationToken>()).Returns(subscription);
        var handler = new PaymentIntentSettledDomainEventHandler(_subscriptions, _bounties, _sender, _unitOfWork);
        var notification = new PaymentIntentSettledDomainEvent(
            Guid.NewGuid(), userId, "Subscription", subscription.Id, 2990m);

        await handler.Handle(notification, CancellationToken.None);
        await handler.Handle(notification, CancellationToken.None);

        subscription.Status.Should().Be(SubscriptionStatus.Active);
        _subscriptions.Received(1).Update(subscription);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
