using FluentAssertions;
using NSubstitute;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Stores;
using PawTrack.Application.Subscriptions.Services;
using PawTrack.Domain.Stores;
using PawTrack.Domain.Subscriptions;

namespace PawTrack.UnitTests.Stores;

public sealed class GetStoreAnalyticsQueryHandlerTests
{
    private readonly IStoreRepository _stores = Substitute.For<IStoreRepository>();
    private readonly IStoreOrderRepository _orders = Substitute.For<IStoreOrderRepository>();
    private readonly ISubscriptionService _subscriptions = Substitute.For<ISubscriptionService>();

    private GetStoreAnalyticsQueryHandler BuildHandler() => new(_stores, _orders, _subscriptions);

    private static Store MakeStore(Guid userId) =>
        Store.Create(userId, "PetShop CR", "desc", "San José", 9.9m, -84.0m, "a@b.com");

    private static StoreOrderMonthlyStats MakeStats() => new(
        TotalOrders: 10,
        DeliveredOrders: 7,
        CancelledOrders: 1,
        TotalRevenueCrc: 70000m,
        AverageOrderValueCrc: 10000m,
        ByDay: [new StoreOrderDayStat(new DateOnly(2026, 8, 1), 3, 30000m)],
        TopProducts: [new StoreTopProductStat(Guid.NewGuid(), "Alimento premium", 5, 25000m)]);

    [Fact]
    public async Task Handle_StoreNotFound_ReturnsFailure()
    {
        var userId = Guid.NewGuid();
        _stores.GetByUserIdAsync(userId, Arg.Any<CancellationToken>()).Returns((Store?)null);

        var result = await BuildHandler().Handle(new GetStoreAnalyticsQuery(userId, 2026, 8), default);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_StoreBasicTier_ReturnsFailure_RequiresUpgrade()
    {
        var userId = Guid.NewGuid();
        _stores.GetByUserIdAsync(userId, Arg.Any<CancellationToken>()).Returns(MakeStore(userId));
        _subscriptions.GetActiveUserTierAsync(userId, Arg.Any<CancellationToken>())
            .Returns(SubscriptionTier.StoreBasic);

        var result = await BuildHandler().Handle(new GetStoreAnalyticsQuery(userId, 2026, 8), default);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Contains("Tienda Plus"));
    }

    [Fact]
    public async Task Handle_StorePlusTier_ReturnsTotalsButNoAdvancedBreakdown()
    {
        var userId = Guid.NewGuid();
        var store = MakeStore(userId);
        _stores.GetByUserIdAsync(userId, Arg.Any<CancellationToken>()).Returns(store);
        _subscriptions.GetActiveUserTierAsync(userId, Arg.Any<CancellationToken>())
            .Returns(SubscriptionTier.StorePlus);
        _orders.GetMonthlyStatsAsync(store.Id, 2026, 8, Arg.Any<Guid?>(), Arg.Any<CancellationToken>()).Returns(MakeStats());

        var result = await BuildHandler().Handle(new GetStoreAnalyticsQuery(userId, 2026, 8), default);

        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalOrders.Should().Be(10);
        result.Value.TotalRevenueCrc.Should().Be(70000m);
        result.Value.ByDay.Should().BeNull();
        result.Value.TopProducts.Should().BeNull();
    }

    [Fact]
    public async Task Handle_StorePartnerTier_ReturnsAdvancedBreakdown()
    {
        var userId = Guid.NewGuid();
        var store = MakeStore(userId);
        _stores.GetByUserIdAsync(userId, Arg.Any<CancellationToken>()).Returns(store);
        _subscriptions.GetActiveUserTierAsync(userId, Arg.Any<CancellationToken>())
            .Returns(SubscriptionTier.StorePartner);
        _orders.GetMonthlyStatsAsync(store.Id, 2026, 8, Arg.Any<Guid?>(), Arg.Any<CancellationToken>()).Returns(MakeStats());

        var result = await BuildHandler().Handle(new GetStoreAnalyticsQuery(userId, 2026, 8), default);

        result.IsSuccess.Should().BeTrue();
        result.Value!.ByDay.Should().HaveCount(1);
        result.Value.TopProducts.Should().HaveCount(1);
        result.Value.TopProducts![0].ProductName.Should().Be("Alimento premium");
    }
}
