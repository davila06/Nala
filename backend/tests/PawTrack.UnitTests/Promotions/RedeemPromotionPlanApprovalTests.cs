using FluentAssertions;
using NSubstitute;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Promotions;
using PawTrack.Application.Promotions.Interfaces;
using PawTrack.Application.Subscriptions.Interfaces;
using PawTrack.Domain.Promotions;
using PawTrack.Domain.Subscriptions;

namespace PawTrack.UnitTests.Promotions;

public sealed class RedeemPromotionPlanApprovalTests
{
    [Fact]
    public async Task Redeem_WhenTargetPlanIsNotCommerciallyApproved_ReturnsFailure()
    {
        var adminId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var code = PromotionCode.CreateFreeTier(
            SubscriptionTier.UserPlus, 10, null, adminId, "test promotion");
        var promotions = Substitute.For<IPromotionCodeRepository>();
        promotions.GetByCodeAsync(code.Code, Arg.Any<CancellationToken>()).Returns(code);
        promotions.HasUserRedeemedAsync(userId, code.Id, Arg.Any<CancellationToken>()).Returns(false);
        promotions.HasUserActivePromoSubscriptionAsync(userId, Arg.Any<CancellationToken>()).Returns(false);

        var plans = Substitute.For<ISubscriptionPlanRepository>();
        plans.GetByTierAsync(SubscriptionTier.UserPlus, Arg.Any<CancellationToken>())
            .Returns(SubscriptionPlan.Create(SubscriptionTier.UserPlus, "Plus", "Plan Plus", 3000m, null));

        var subscriptions = Substitute.For<ISubscriptionRepository>();
        var handler = new RedeemPromotionCodeCommandHandler(
            promotions, subscriptions, plans, Substitute.For<IUnitOfWork>());

        var result = await handler.Handle(new RedeemPromotionCodeCommand(code.Code, userId), default);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain("El plan seleccionado no está aprobado para publicación comercial.");
        await subscriptions.DidNotReceive().AddAsync(Arg.Any<Subscription>(), Arg.Any<CancellationToken>());
    }
}
