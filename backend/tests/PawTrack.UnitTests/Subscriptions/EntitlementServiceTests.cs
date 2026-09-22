using FluentAssertions;
using NSubstitute;
using PawTrack.Application.Subscriptions.Interfaces;
using PawTrack.Application.Subscriptions.Services;
using PawTrack.Domain.Subscriptions;
using PawTrack.Infrastructure.Subscriptions;

namespace PawTrack.UnitTests.Subscriptions;

public sealed class EntitlementServiceTests
{
    [Fact]
    public async Task Consume_is_idempotent_for_the_same_key()
    {
        var subjectId = Guid.NewGuid();
        var subscription = Subscription.CreateFromPromotion(
            subjectId, SubscriptionTier.UserPlus, 1, Guid.NewGuid());
        var plan = SubscriptionPlan.Create(
            SubscriptionTier.UserPlus, "Plus", "Plus", 2990m, null);
        var entitlement = PlanEntitlement.Create(
            plan.Id, "AiMatchesPerCycle", EntitlementValueType.Numeric, 10m, unit: "cycle", resetPeriod: "monthly");
        var subscriptions = Substitute.For<ISubscriptionRepository>();
        var plans = Substitute.For<ISubscriptionPlanRepository>();
        var entitlements = Substitute.For<IEntitlementRepository>();
        var unitOfWork = Substitute.For<PawTrack.Application.Common.Interfaces.IUnitOfWork>();

        subscriptions.GetActiveForSubjectAsync(subjectId, Arg.Any<CancellationToken>()).Returns(subscription);
        plans.GetByTierAsync(SubscriptionTier.UserPlus, Arg.Any<CancellationToken>()).Returns(plan);
        entitlements.GetActiveForPlanAsync(plan.Id, Arg.Any<CancellationToken>()).Returns([entitlement]);
        entitlements.GetConsumedAsync(subjectId, "AiMatchesPerCycle", Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), null, null, Arg.Any<CancellationToken>()).Returns(1m);
        entitlements.FindByIdempotencyKeyAsync(subjectId, "AiMatchesPerCycle", "request-1", Arg.Any<CancellationToken>())
            .Returns((EntitlementConsumption?)null);

        var service = new EntitlementService(subscriptions, plans, entitlements, unitOfWork);
        var context = new EntitlementContext("sighting", Guid.NewGuid());

        var first = await service.ConsumeAsync(subjectId, "AiMatchesPerCycle", 1m, "request-1", context);
        entitlements.FindByIdempotencyKeyAsync(subjectId, "AiMatchesPerCycle", "request-1", Arg.Any<CancellationToken>())
            .Returns(EntitlementConsumption.Create(
                subjectId, subscription.Id, "AiMatchesPerCycle", 1m, "request-1",
                first.CycleStart!.Value, first.CycleEnd!.Value));
        var second = await service.ConsumeAsync(subjectId, "AiMatchesPerCycle", 1m, "request-1", context);

        first.Consumed.Should().BeTrue();
        second.AlreadyConsumed.Should().BeTrue();
        await entitlements.Received(1).AddAsync(Arg.Any<EntitlementConsumption>(), Arg.Any<CancellationToken>());
    }
}
