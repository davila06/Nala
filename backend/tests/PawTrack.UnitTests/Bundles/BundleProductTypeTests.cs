using FluentAssertions;
using PawTrack.Application.Bundles;
using PawTrack.Domain.Bundles;

namespace PawTrack.UnitTests.Bundles;

public sealed class BundleProductTypeTests
{
    [Theory]
    [InlineData(BundleProductType.QrPlate, 4_500)]
    [InlineData(BundleProductType.SiliconeTag, 5_500)]
    [InlineData(BundleProductType.NfcQrCombo, 12_000)]
    [InlineData(BundleProductType.EmergencyPack, 7_000)]
    [InlineData(BundleProductType.CollarGpsPlus, 49_900)]
    [InlineData(BundleProductType.CollarTagGps, 39_900)]
    public void GetPrice_ReturnsCorrectAmount(BundleProductType type, decimal expected)
    {
        BundlePrices.GetPrice(type).Should().Be(expected);
    }

    [Fact]
    public void GetPrice_WhenRequiresInvoiceIsTrue_AddsThirteenPercentIva()
    {
        // 49,900 * 1.13 = 56,387
        BundlePrices.GetPrice(BundleProductType.CollarGpsPlus, requiresInvoice: true)
            .Should().Be(56_387m);

        // 4,500 * 1.13 = 5,085
        BundlePrices.GetPrice(BundleProductType.QrPlate, requiresInvoice: true)
            .Should().Be(5_085m);
    }

    [Fact]
    public void CollarTagGps_HasLabel()
    {
        var order = BundleOrder.Create(
            Guid.NewGuid(), CollarModel.TractiveGPSDog4,
            paymentReference: "REF-001",
            amountCrc: 39_900m,
            shippingFullName: "Test User",
            shippingAddress: "San José",
            shippingCanton: "San José",
            shippingPhone: "+50688880000",
            deliveryNotes: null,
            productType: BundleProductType.CollarTagGps);

        var dto = BundleOrderDto.FromDomain(order);

        dto.ProductType.Should().Be("CollarTagGps");
        dto.ProductTypeLabel.Should().Contain("CollarTag");
    }
}
