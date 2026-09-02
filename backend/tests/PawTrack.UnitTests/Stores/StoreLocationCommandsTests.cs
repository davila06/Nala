using FluentAssertions;
using NSubstitute;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Stores;
using PawTrack.Application.Subscriptions.Services;
using PawTrack.Domain.Stores;
using PawTrack.Domain.Subscriptions;

namespace PawTrack.UnitTests.Stores;

public sealed class StoreLocationCommandsTests
{
    private readonly IStoreRepository _stores = Substitute.For<IStoreRepository>();
    private readonly ISubscriptionService _subscriptions = Substitute.For<ISubscriptionService>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    private static Store MakeStore(Guid userId) =>
        Store.Create(userId, "PetShop CR", "desc", "San José", 9.9m, -84.0m, "a@b.com");

    // ── Create ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Create_NonPartnerTier_ReturnsFailure()
    {
        var userId = Guid.NewGuid();
        _stores.GetByUserIdAsync(userId, Arg.Any<CancellationToken>()).Returns(MakeStore(userId));
        _subscriptions.GetActiveUserTierAsync(userId, Arg.Any<CancellationToken>())
            .Returns(SubscriptionTier.StorePlus);

        var handler = new CreateStoreLocationCommandHandler(_stores, _subscriptions, _uow);
        var result = await handler.Handle(
            new CreateStoreLocationCommand(userId, "Sucursal", "Dir", 9.9m, -84m, null), default);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Contains("Partner"));
    }

    [Fact]
    public async Task Create_FirstLocation_IsMarkedPrimary()
    {
        var userId = Guid.NewGuid();
        var store = MakeStore(userId);
        _stores.GetByUserIdAsync(userId, Arg.Any<CancellationToken>()).Returns(store);
        _subscriptions.GetActiveUserTierAsync(userId, Arg.Any<CancellationToken>())
            .Returns(SubscriptionTier.StorePartner);
        _stores.GetLocationsByStoreAsync(store.Id, Arg.Any<CancellationToken>())
            .Returns(new List<StoreLocation>());

        var handler = new CreateStoreLocationCommandHandler(_stores, _subscriptions, _uow);
        var result = await handler.Handle(
            new CreateStoreLocationCommand(userId, "Matriz", "Centro", 9.9m, -84m, null), default);

        result.IsSuccess.Should().BeTrue();
        result.Value!.IsPrimary.Should().BeTrue();
    }

    [Fact]
    public async Task Create_SecondLocation_IsNotPrimary()
    {
        var userId = Guid.NewGuid();
        var store = MakeStore(userId);
        var existingPrimary = StoreLocation.Create(store.Id, "Matriz", "Centro", 9.9m, -84m, null, isPrimary: true);
        _stores.GetByUserIdAsync(userId, Arg.Any<CancellationToken>()).Returns(store);
        _subscriptions.GetActiveUserTierAsync(userId, Arg.Any<CancellationToken>())
            .Returns(SubscriptionTier.StorePartner);
        _stores.GetLocationsByStoreAsync(store.Id, Arg.Any<CancellationToken>())
            .Returns(new List<StoreLocation> { existingPrimary });

        var handler = new CreateStoreLocationCommandHandler(_stores, _subscriptions, _uow);
        var result = await handler.Handle(
            new CreateStoreLocationCommand(userId, "Sucursal 2", "Norte", 9.9m, -84m, null), default);

        result.IsSuccess.Should().BeTrue();
        result.Value!.IsPrimary.Should().BeFalse();
    }

    // ── Deactivate ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Deactivate_OtherStoresLocation_ReturnsFailure()
    {
        var userId = Guid.NewGuid();
        var store = MakeStore(userId);
        var foreignLocation = StoreLocation.Create(Guid.NewGuid(), "Ajena", "Otra", 9.9m, -84m, null);

        _stores.GetByUserIdAsync(userId, Arg.Any<CancellationToken>()).Returns(store);
        _subscriptions.GetActiveUserTierAsync(userId, Arg.Any<CancellationToken>())
            .Returns(SubscriptionTier.StorePartner);
        _stores.GetLocationByIdAsync(foreignLocation.Id, Arg.Any<CancellationToken>()).Returns(foreignLocation);

        var handler = new SetStoreLocationActiveCommandHandler(_stores, _subscriptions, _uow);
        var result = await handler.Handle(
            new SetStoreLocationActiveCommand(userId, foreignLocation.Id, false), default);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Deactivate_PrimaryLocation_ReturnsFailure()
    {
        var userId = Guid.NewGuid();
        var store = MakeStore(userId);
        var primary = StoreLocation.Create(store.Id, "Matriz", "Centro", 9.9m, -84m, null, isPrimary: true);

        _stores.GetByUserIdAsync(userId, Arg.Any<CancellationToken>()).Returns(store);
        _subscriptions.GetActiveUserTierAsync(userId, Arg.Any<CancellationToken>())
            .Returns(SubscriptionTier.StorePartner);
        _stores.GetLocationByIdAsync(primary.Id, Arg.Any<CancellationToken>()).Returns(primary);

        var handler = new SetStoreLocationActiveCommandHandler(_stores, _subscriptions, _uow);
        var result = await handler.Handle(new SetStoreLocationActiveCommand(userId, primary.Id, false), default);

        result.IsFailure.Should().BeTrue();
    }
}
