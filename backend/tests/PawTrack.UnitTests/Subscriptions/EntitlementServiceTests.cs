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
    public async Task Authorize_FreeFamilyAccount_AllowsOnlyTheOwnerSeat()
    {
        var subjectId = Guid.NewGuid();
        var subscriptions = Substitute.For<ISubscriptionRepository>();
        var entitlements = Substitute.For<IEntitlementRepository>();
        subscriptions.GetActiveForSubjectAsync(subjectId, Arg.Any<CancellationToken>())
            .Returns((Subscription?)null);
        entitlements.GetConsumedAsync(
                subjectId, "MaxFamilyMembers", Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(),
                "family-member", Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(0m);

        var service = new EntitlementService(
            subscriptions,
            Substitute.For<ISubscriptionPlanRepository>(),
            entitlements,
            Substitute.For<PawTrack.Application.Common.Interfaces.IUnitOfWork>());

        var decision = await service.AuthorizeAsync(
            subjectId, "MaxFamilyMembers", 1m,
            new EntitlementContext("family-member", Guid.NewGuid()));

        decision.Limit.Should().Be(1m);
        decision.Remaining.Should().Be(1m);
    }

    [Fact]
    public async Task Authorize_FreePlan_AllowsOneActiveLostCase()
    {
        var subjectId = Guid.NewGuid();
        var caseId = Guid.NewGuid();
        var subscriptions = Substitute.For<ISubscriptionRepository>();
        var entitlements = Substitute.For<IEntitlementRepository>();
        subscriptions.GetActiveForSubjectAsync(subjectId, Arg.Any<CancellationToken>())
            .Returns((Subscription?)null);
        entitlements.GetConsumedAsync(
                subjectId, "MaxActiveLostCases", Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(),
                "lost-pet-case", caseId, Arg.Any<CancellationToken>())
            .Returns(0m);
        var service = new EntitlementService(
            subscriptions,
            Substitute.For<ISubscriptionPlanRepository>(),
            entitlements,
            Substitute.For<PawTrack.Application.Common.Interfaces.IUnitOfWork>());

        var decision = await service.AuthorizeAsync(
            subjectId, "MaxActiveLostCases", 1m,
            new EntitlementContext("lost-pet-case", caseId));

        decision.Allowed.Should().BeTrue();
        decision.Limit.Should().Be(1m);
    }

    [Fact]
    public async Task Authorize_FreePlan_DeniesActiveVetReminders()
    {
        var subjectId = Guid.NewGuid();
        var subscriptions = Substitute.For<ISubscriptionRepository>();
        var entitlements = Substitute.For<IEntitlementRepository>();
        subscriptions.GetActiveForSubjectAsync(subjectId, Arg.Any<CancellationToken>())
            .Returns((Subscription?)null);
        entitlements.GetConsumedAsync(
                subjectId, "MaxActiveVetReminders", Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(),
                "pet", Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(0m);
        var service = new EntitlementService(
            subscriptions,
            Substitute.For<ISubscriptionPlanRepository>(),
            entitlements,
            Substitute.For<PawTrack.Application.Common.Interfaces.IUnitOfWork>());

        var decision = await service.AuthorizeAsync(
            subjectId, "MaxActiveVetReminders", 1m,
            new EntitlementContext("pet", Guid.NewGuid()));

        decision.Allowed.Should().BeFalse();
        decision.Limit.Should().Be(0m);
    }

    [Fact]
    public async Task Authorize_FreePlan_DoesNotIncludeAiMatching()
    {
        var subjectId = Guid.NewGuid();
        var subscriptions = Substitute.For<ISubscriptionRepository>();
        subscriptions.GetActiveForSubjectAsync(subjectId, Arg.Any<CancellationToken>())
            .Returns((Subscription?)null);
        var service = new EntitlementService(
            subscriptions,
            Substitute.For<ISubscriptionPlanRepository>(),
            Substitute.For<IEntitlementRepository>(),
            Substitute.For<PawTrack.Application.Common.Interfaces.IUnitOfWork>());

        var decision = await service.AuthorizeAsync(
            subjectId, "AiMatchesPerCycle", 1m, new EntitlementContext("visual-match"));

        decision.Allowed.Should().BeFalse();
    }

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
