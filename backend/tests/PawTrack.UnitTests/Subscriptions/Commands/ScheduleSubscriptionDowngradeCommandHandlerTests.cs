using FluentAssertions;
using NSubstitute;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Subscriptions.Commands.ScheduleSubscriptionDowngrade;
using PawTrack.Application.Subscriptions.Interfaces;
using PawTrack.Domain.Subscriptions;

namespace PawTrack.UnitTests.Subscriptions.Commands;

public sealed class ScheduleSubscriptionDowngradeCommandHandlerTests
{
    private readonly ISubscriptionRepository subscriptions = Substitute.For<ISubscriptionRepository>();
    private readonly ISubscriptionPlanRepository plans = Substitute.For<ISubscriptionPlanRepository>();
    private readonly IPaymentService payments = Substitute.For<IPaymentService>();
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();

    [Fact]
    public async Task Handle_FamiliaToPlus_CreatesPendingPlusStartingAtCurrentExpiry()
    {
        var userId = Guid.NewGuid();
        var current = Subscription.CreateForUser(userId, SubscriptionTier.UserFamilia, "FAM12345", 4990m);
        current.Activate();
        var expiry = current.ExpiresAt!.Value;
        var plan = SubscriptionPlan.Create(SubscriptionTier.UserPlus, "Plus", "Plus plan", 2990m, null);
        payments.GenerateReference().Returns("PLUS1234");
        subscriptions.GetByIdAsync(current.Id, Arg.Any<CancellationToken>()).Returns(current);
        subscriptions.GetPendingForUserAsync(userId, Arg.Any<CancellationToken>()).Returns((Subscription?)null);
        plans.GetByTierAsync(SubscriptionTier.UserPlus, Arg.Any<CancellationToken>()).Returns(plan);

        var result = await new ScheduleSubscriptionDowngradeCommandHandler(
            subscriptions, plans, payments, unitOfWork).Handle(
                new ScheduleSubscriptionDowngradeCommand(current.Id, userId, SubscriptionTier.UserPlus), default);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Tier.Should().Be(SubscriptionTier.UserPlus);
        result.Value.StartsAt.Should().Be(expiry);
        result.Value.IsActive.Should().BeFalse();
        current.CancellationRequestedAt.Should().NotBeNull();
        await subscriptions.Received(1).AddAsync(
            Arg.Is<Subscription>(s => s.Tier == SubscriptionTier.UserPlus && s.StartsAt == expiry),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NonOwner_ReturnsAccessDenied()
    {
        var ownerId = Guid.NewGuid();
        var current = Subscription.CreateForUser(ownerId, SubscriptionTier.UserFamilia, "FAM12346", 4990m);
        current.Activate();
        subscriptions.GetByIdAsync(current.Id, Arg.Any<CancellationToken>()).Returns(current);

        var result = await new ScheduleSubscriptionDowngradeCommandHandler(
            subscriptions, plans, payments, unitOfWork).Handle(
                new ScheduleSubscriptionDowngradeCommand(current.Id, Guid.NewGuid(), SubscriptionTier.UserPlus), default);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain("Access denied.");
    }
}
