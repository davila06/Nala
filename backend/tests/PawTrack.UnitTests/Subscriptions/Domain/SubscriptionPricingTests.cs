using FluentAssertions;
using PawTrack.Domain.Subscriptions;

namespace PawTrack.UnitTests.Subscriptions.Domain;

public sealed class SubscriptionPricingTests
{
    [Theory]
    [InlineData(1, 2990)]
    [InlineData(3, 8970)]
    [InlineData(6, 17940)]
    [InlineData(12, 28704)]
    public void CalculateTermPriceCrc_AppliesAnnualDiscountOnlyAtTwelveMonths(
        int billingMonths,
        decimal expectedAmount)
    {
        var amount = SubscriptionPricing.CalculateTermPriceCrc(2990m, billingMonths);

        amount.Should().Be(expectedAmount);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    [InlineData(4)]
    [InlineData(11)]
    [InlineData(13)]
    public void CalculateTermPriceCrc_RejectsUnsupportedBillingMonths(int billingMonths)
    {
        var act = () => SubscriptionPricing.CalculateTermPriceCrc(2990m, billingMonths);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

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

    [Fact]
    public void CalculateIvaAmountCrc_CalculatesThirteenPercentCorrectly()
    {
        // 2990 * 0.13 = 388.70
        var iva = SubscriptionPricing.CalculateIvaAmountCrc(2990m);
        iva.Should().Be(388.70m);

        // 35000 * 0.13 = 4550.00
        var ivaClinic = SubscriptionPricing.CalculateIvaAmountCrc(35000m);
        ivaClinic.Should().Be(4550.00m);
    }

    [Fact]
    public void CalculateTotalWithIvaCrc_AddsThirteenPercentToCostOfService()
    {
        // 2990 + 388.70 = 3378.70
        var total = SubscriptionPricing.CalculateTotalWithIvaCrc(2990m);
        total.Should().Be(3378.70m);

        // 35000 + 4550 = 39550.00
        var totalClinic = SubscriptionPricing.CalculateTotalWithIvaCrc(35000m);
        totalClinic.Should().Be(39550.00m);
    }

    [Fact]
    public void GetEffectivePriceCrc_WhenRequiresInvoiceIsTrue_AddsThirteenPercentIva()
    {
        var withInvoice = SubscriptionPricing.GetEffectivePriceCrc(2990m, requiresInvoice: true);
        withInvoice.Should().Be(3378.70m);
    }

    [Fact]
    public void GetEffectivePriceCrc_WhenRequiresInvoiceIsFalse_ReturnsBaseCost()
    {
        var withoutInvoice = SubscriptionPricing.GetEffectivePriceCrc(2990m, requiresInvoice: false);
        withoutInvoice.Should().Be(2990m);
    }
}
