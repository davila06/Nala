using FluentAssertions;
using PawTrack.Domain.Subscriptions;

namespace PawTrack.UnitTests.Subscriptions.Domain;

public sealed class SubscriptionPricingTests
{
    [Theory]
    [InlineData(1, 1000)]
    [InlineData(3, 3000)]
    [InlineData(6, 6000)]
    [InlineData(12, 9600)]
    public void CalculateTermPriceCrc_AppliesAnnualDiscountOnlyAtTwelveMonths(
        int billingMonths,
        decimal expectedAmount)
    {
        var amount = SubscriptionPricing.CalculateTermPriceCrc(1000m, billingMonths);

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
        var act = () => SubscriptionPricing.CalculateTermPriceCrc(1000m, billingMonths);

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
    [InlineData(SubscriptionTier.MuniBasica)]
    [InlineData(SubscriptionTier.MuniFull)]
    [InlineData(SubscriptionTier.MuniRedRegional)]
    public void IsPaidTier_ReturnsTrueForBillableTiers(SubscriptionTier tier)
    {
        SubscriptionPricing.IsPaidTier(tier).Should().BeTrue();
    }

    [Theory]
    [InlineData(SubscriptionTier.Free)]
    [InlineData(SubscriptionTier.ClinicBasic)]
    [InlineData(SubscriptionTier.StoreBasic)]
    [InlineData(SubscriptionTier.ShelterBasic)]
    public void IsPaidTier_ReturnsFalseForBaseTiers(SubscriptionTier tier)
    {
        SubscriptionPricing.IsPaidTier(tier).Should().BeFalse();
    }

    [Fact]
    public void CalculateIvaAmountCrc_CalculatesThirteenPercentCorrectly()
    {
        // 1000 * 0.13 = 130
        var iva = SubscriptionPricing.CalculateIvaAmountCrc(1000m);
        iva.Should().Be(130m);

        // 35000 * 0.13 = 4550.00
        var ivaClinic = SubscriptionPricing.CalculateIvaAmountCrc(35000m);
        ivaClinic.Should().Be(4550.00m);
    }

    [Fact]
    public void CalculateTotalWithIvaCrc_AddsThirteenPercentToCostOfService()
    {
        // 1000 + 130 = 1130
        var total = SubscriptionPricing.CalculateTotalWithIvaCrc(1000m);
        total.Should().Be(1130m);

        // 35000 + 4550 = 39550.00
        var totalClinic = SubscriptionPricing.CalculateTotalWithIvaCrc(35000m);
        totalClinic.Should().Be(39550.00m);
    }

    [Fact]
    public void GetEffectivePriceCrc_WhenRequiresInvoiceIsTrue_AddsThirteenPercentIva()
    {
        var withInvoice = SubscriptionPricing.GetEffectivePriceCrc(1000m, requiresInvoice: true);
        withInvoice.Should().Be(1130m);
    }

    [Fact]
    public void GetEffectivePriceCrc_WhenRequiresInvoiceIsFalse_ReturnsBaseCost()
    {
        var withoutInvoice = SubscriptionPricing.GetEffectivePriceCrc(1000m, requiresInvoice: false);
        withoutInvoice.Should().Be(1000m);
    }
}
