using FluentAssertions;
using NSubstitute;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Stores.Events;
using PawTrack.Domain.Stores;
using PawTrack.Domain.Stores.Events;

namespace PawTrack.UnitTests.Stores;

public sealed class StoreOrderLifecycleDomainEventHandlerTests
{
    [Fact]
    public async Task PaymentReported_NotifiesStoreToVerifyOutsidePawTrack()
    {
        var store = Store.Create(Guid.NewGuid(), "La Huella", "Tienda", "San Jose", 9.9m, -84m, "store@example.test");
        var storeRepository = Substitute.For<IStoreRepository>();
        storeRepository.GetByIdAsync(store.Id, Arg.Any<CancellationToken>()).Returns(store);
        var dispatcher = Substitute.For<INotificationDispatcher>();
        var handler = new StoreOrderLifecycleDomainEventHandler(storeRepository, dispatcher);
        var domainEvent = new StoreOrderLifecycleDomainEvent(
            Guid.NewGuid(), Guid.NewGuid(), store.Id, Guid.NewGuid(), StoreOrderStatus.PaymentReported,
            OrderFulfillmentType.Pickup, 3500m, DateTimeOffset.UtcNow);

        await handler.Handle(domainEvent, CancellationToken.None);

        await dispatcher.Received(1).DispatchStoreOrderLifecycleAsync(
            domainEvent.EventId,
            store.UserId,
            domainEvent.OrderId,
            Arg.Is<string>(title => title.Contains("pago", StringComparison.OrdinalIgnoreCase)),
            Arg.Is<string>(body => body.Contains("fuera de PawTrack", StringComparison.OrdinalIgnoreCase)),
            "/tienda/portal/ordenes",
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReadyForPickup_NotifiesCustomer()
    {
        var store = Store.Create(Guid.NewGuid(), "La Huella", "Tienda", "San Jose", 9.9m, -84m, "store@example.test");
        var storeRepository = Substitute.For<IStoreRepository>();
        storeRepository.GetByIdAsync(store.Id, Arg.Any<CancellationToken>()).Returns(store);
        var dispatcher = Substitute.For<INotificationDispatcher>();
        var handler = new StoreOrderLifecycleDomainEventHandler(storeRepository, dispatcher);
        var customerId = Guid.NewGuid();
        var domainEvent = new StoreOrderLifecycleDomainEvent(
            Guid.NewGuid(), Guid.NewGuid(), store.Id, customerId, StoreOrderStatus.ReadyForPickup,
            OrderFulfillmentType.Pickup, 3500m, DateTimeOffset.UtcNow);

        await handler.Handle(domainEvent, CancellationToken.None);

        await dispatcher.Received(1).DispatchStoreOrderLifecycleAsync(
            domainEvent.EventId,
            customerId,
            domainEvent.OrderId,
            Arg.Is<string>(title => title.Contains("retirar", StringComparison.OrdinalIgnoreCase)),
            Arg.Is<string>(body => body.Contains("retirar", StringComparison.OrdinalIgnoreCase)),
            "/mis-pedidos",
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MissingStore_DoesNotDispatchNotification()
    {
        var storeRepository = Substitute.For<IStoreRepository>();
        var dispatcher = Substitute.For<INotificationDispatcher>();
        var handler = new StoreOrderLifecycleDomainEventHandler(storeRepository, dispatcher);
        var domainEvent = new StoreOrderLifecycleDomainEvent(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), StoreOrderStatus.PaymentReported,
            OrderFulfillmentType.Pickup, 3500m, DateTimeOffset.UtcNow);

        await handler.Handle(domainEvent, CancellationToken.None);

        await dispatcher.DidNotReceive().DispatchStoreOrderLifecycleAsync(
            Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
