using FluentAssertions;
using NSubstitute;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Municipalities.Interfaces;
using PawTrack.Application.Subscriptions.Commands.AdminActivateSubscription;
using PawTrack.Application.Subscriptions.Interfaces;
using PawTrack.Domain.Audit;
using PawTrack.Domain.Subscriptions;

namespace PawTrack.UnitTests.Subscriptions.Commands;

public sealed class AdminActivateSubscriptionAnnualTests
{
    [Fact]
    public async Task Handle_MunicipalPlan_DefaultActivation_ExpiresAfterOneYear()
    {
        var subscription = Subscription.CreateForUser(
            Guid.NewGuid(),
            SubscriptionTier.MuniFull,
            "ABCD1234",
            300000m);
        var subscriptions = Substitute.For<ISubscriptionRepository>();
        subscriptions.GetByIdAsync(subscription.Id, Arg.Any<CancellationToken>())
            .Returns(subscription);
        var plans = Substitute.For<ISubscriptionPlanRepository>();
        var plan = SubscriptionPlan.Create(SubscriptionTier.MuniFull, "Municipal Full", "Municipality", null, 300_000m);
        plan.ApproveForCommercialPublication(Guid.NewGuid(), "TEST-APPROVAL");
        plans.GetByTierAsync(SubscriptionTier.MuniFull, Arg.Any<CancellationToken>()).Returns(plan);
        var uow = Substitute.For<IUnitOfWork>();
        uow.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

        var handler = new AdminActivateSubscriptionCommandHandler(
            subscriptions,
            plans,
            Substitute.For<IClinicRepository>(),
            Substitute.For<IStoreRepository>(),
            Substitute.For<IMunicipalProfileRepository>(),
            Substitute.For<IAuditLogRepository>(),
            uow);

        var before = DateTimeOffset.UtcNow.AddYears(1);
        var result = await handler.Handle(
            new AdminActivateSubscriptionCommand(subscription.Id),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        subscription.ExpiresAt.Should().BeCloseTo(before, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task Handle_UnapprovedPlan_ReturnsFailureWithoutActivating()
    {
        var subscription = Subscription.CreateForUser(
            Guid.NewGuid(),
            SubscriptionTier.MuniFull,
            "PENDING1",
            300000m);
        var subscriptions = Substitute.For<ISubscriptionRepository>();
        subscriptions.GetByIdAsync(subscription.Id, Arg.Any<CancellationToken>())
            .Returns(subscription);
        var plans = Substitute.For<ISubscriptionPlanRepository>();
        plans.GetByTierAsync(SubscriptionTier.MuniFull, Arg.Any<CancellationToken>())
            .Returns(SubscriptionPlan.Create(SubscriptionTier.MuniFull, "Municipal Full", "Municipality", null, 300_000m));

        var handler = new AdminActivateSubscriptionCommandHandler(
            subscriptions,
            plans,
            Substitute.For<IClinicRepository>(),
            Substitute.For<IStoreRepository>(),
            Substitute.For<IMunicipalProfileRepository>(),
            Substitute.For<IAuditLogRepository>(),
            Substitute.For<IUnitOfWork>());

        var result = await handler.Handle(new AdminActivateSubscriptionCommand(subscription.Id), default);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain("Subscription plan is not approved for commercial publication.");
        subscription.Status.Should().Be(SubscriptionStatus.PendingPayment);
        subscriptions.DidNotReceive().Update(subscription);
    }
}
