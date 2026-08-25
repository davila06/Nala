using FluentAssertions;
using PawTrack.Domain.Subscriptions;

namespace PawTrack.UnitTests.Subscriptions.Domain;

public sealed class SubscriptionPricingTests
{
    [Theory]
    [InlineData(SubscriptionTier.UserPlus)]
    [InlineData(SubscriptionTier.UserFamilia)]
    [InlineData(SubscriptionTier.ClinicPlus)]
    [InlineData(SubscriptionTier.ClinicPartner)]
    [InlineData(SubscriptionTier.StorePlus)]
    [InlineData(SubscriptionTier.StorePartner)]
    [InlineData(SubscriptionTier.ShelterPlus)]
    public void TryGetMonthlyPriceCrc_PaidTier_ReturnsPositiveAmount(SubscriptionTier tier)
    {
        var found = SubscriptionPricing.TryGetMonthlyPriceCrc(tier, out var amount);

        found.Should().BeTrue();
        amount.Should().BeGreaterThan(0);
        SubscriptionPricing.IsPaidTier(tier).Should().BeTrue();
    }

    [Theory]
    [InlineData(SubscriptionTier.Free)]
    [InlineData(SubscriptionTier.ClinicBasic)]
    [InlineData(SubscriptionTier.StoreBasic)]
    [InlineData(SubscriptionTier.ShelterBasic)]
    public void TryGetMonthlyPriceCrc_FreeTier_ReturnsFalse(SubscriptionTier tier)
    {
        var found = SubscriptionPricing.TryGetMonthlyPriceCrc(tier, out _);

        found.Should().BeFalse();
        SubscriptionPricing.IsPaidTier(tier).Should().BeFalse();
    }
}
