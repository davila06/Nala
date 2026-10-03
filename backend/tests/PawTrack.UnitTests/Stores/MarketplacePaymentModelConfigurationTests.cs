using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using PawTrack.Domain.ServiceProviders;
using PawTrack.Domain.Stores;
using PawTrack.Infrastructure.Persistence;

namespace PawTrack.UnitTests.Stores;

public sealed class MarketplacePaymentModelConfigurationTests
{
    [Fact]
    public void StoreOrderHasCustomerScopedFilteredIdempotencyIndex()
    {
        using var db = CreateContext();
        var entity = db.Model.FindEntityType(typeof(StoreOrder))!;
        var index = entity.GetIndexes().Single(candidate =>
            candidate.Properties.Select(property => property.Name)
                .SequenceEqual([nameof(StoreOrder.CustomerId), nameof(StoreOrder.IdempotencyKey)]));

        index.IsUnique.Should().BeTrue();
        index.GetFilter().Should().Be("[IdempotencyKey] IS NOT NULL");
        entity.FindProperty(nameof(StoreOrder.RequestHash))!.GetMaxLength().Should().Be(64);
    }

    [Fact]
    public void ProviderPaymentRefundFieldsHaveBoundedReferenceAndCurrencyPrecision()
    {
        using var db = CreateContext();
        var entity = db.Model.FindEntityType(typeof(ProviderPayment))!;

        entity.FindProperty(nameof(ProviderPayment.RefundReference))!.GetMaxLength().Should().Be(200);
        entity.FindProperty(nameof(ProviderPayment.RefundedAmountCrc))!.GetColumnType().Should().Be("decimal(12,2)");
    }

    private static PawTrackDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<PawTrackDbContext>()
            .UseSqlServer(
                "Server=(localdb)\\MSSQLLocalDB;Database=ModelOnly;Trusted_Connection=True;",
                sqlOptions => sqlOptions.UseNetTopologySuite())
            .Options;
        return new PawTrackDbContext(options);
    }
}