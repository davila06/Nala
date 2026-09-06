using FluentAssertions;
using NSubstitute;
using PawTrack.Application.Subscriptions.Commands.CreateSubscriptionPlan;
using PawTrack.Application.Subscriptions.Interfaces;
using PawTrack.Domain.Subscriptions;

namespace PawTrack.UnitTests.Subscriptions.Commands;

public sealed class ManageSubscriptionPlansCommandHandlerTests
{
    [Fact]
    public async Task Create_PaidPlan_PersistsCatalogEntry()
    {
        var repository = Substitute.For<ISubscriptionPlanRepository>();
        repository.GetByTierAsync(SubscriptionTier.UserPlus, Arg.Any<CancellationToken>())
            .Returns((SubscriptionPlan?)null);
        var handler = new CreateSubscriptionPlanCommandHandler(repository);

        var result = await handler.Handle(
            new CreateSubscriptionPlanCommand(
                SubscriptionTier.UserPlus,
                "Plus",
                "Plan para dueños avanzados",
                2990m,
                null),
            default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Tier.Should().Be(SubscriptionTier.UserPlus);
        result.Value.MonthlyPriceCrc.Should().Be(2990m);
        await repository.Received(1).AddAsync(
            Arg.Is<SubscriptionPlan>(plan => plan.Tier == SubscriptionTier.UserPlus),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Create_FreeState_ReturnsValidationFailure()
    {
        var repository = Substitute.For<ISubscriptionPlanRepository>();
        var handler = new CreateSubscriptionPlanCommandHandler(repository);

        var result = await handler.Handle(
            new CreateSubscriptionPlanCommand(
                SubscriptionTier.Free,
                "Explorador",
                "Plan gratuito",
                null,
                null),
            default);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain("Only paid subscription tiers can be managed as plans.");
        await repository.DidNotReceive().AddAsync(
            Arg.Any<SubscriptionPlan>(),
            Arg.Any<CancellationToken>());
    }
}
