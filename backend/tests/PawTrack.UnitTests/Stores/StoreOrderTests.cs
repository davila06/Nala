using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Stores;
using PawTrack.Application.Subscriptions.Interfaces;
using PawTrack.Application.Subscriptions.Services;
using PawTrack.Domain.Stores;
using PawTrack.Domain.Subscriptions;

namespace PawTrack.UnitTests.Stores;

// ── StoreOrder domain: state machine ─────────────────────────────────────────

public sealed class StoreOrderStateMachineTests
{
    private static StoreOrder MakeOrder(OrderFulfillmentType fulfillment = OrderFulfillmentType.Pickup)
    {
        var lines = new List<(Guid, string, int, decimal)>
        {
            (Guid.NewGuid(), "Product A", 2, 1500m)
        };
        return StoreOrder.Place(Guid.NewGuid(), Guid.NewGuid(), "REF12345",
            fulfillment, null, null, lines);
    }

    [Fact]
    public void NewOrder_HasPendingPaymentStatus()
    {
        var order = MakeOrder();
        order.Status.Should().Be(StoreOrderStatus.PendingPayment);
    }

    [Fact]
    public void ReportPayment_FromPendingPayment_Transitions()
    {
        var order = MakeOrder();
        order.ReportPayment();
        order.Status.Should().Be(StoreOrderStatus.PaymentReported);
        order.PaymentReportedByCustomer.Should().BeTrue();
    }

    [Fact]
    public void ReportPayment_WhenAlreadyReported_Throws()
    {
        var order = MakeOrder();
        order.ReportPayment();
        var act = () => order.ReportPayment();
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Confirm_FromPaymentReported_Transitions()
    {
        var order = MakeOrder();
        order.ReportPayment();
        order.Confirm("Looking good");
        order.Status.Should().Be(StoreOrderStatus.Confirmed);
        order.ConfirmedAt.Should().NotBeNull();
    }

    [Fact]
    public void Confirm_FromPendingPayment_Throws()
    {
        var order = MakeOrder();
        var act = () => order.Confirm();
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void UpdateStatus_ValidPickupPath_Transitions()
    {
        var order = MakeOrder(OrderFulfillmentType.Pickup);
        order.ReportPayment();
        order.Confirm();
        order.UpdateStatus(StoreOrderStatus.Preparing);
        order.UpdateStatus(StoreOrderStatus.ReadyForPickup);
        order.UpdateStatus(StoreOrderStatus.Delivered);
        order.Status.Should().Be(StoreOrderStatus.Delivered);
        order.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public void UpdateStatus_ValidDeliveryPath_Transitions()
    {
        var order = MakeOrder(OrderFulfillmentType.Delivery);
        order.ReportPayment();
        order.Confirm();
        order.UpdateStatus(StoreOrderStatus.Preparing);
        order.UpdateStatus(StoreOrderStatus.OutForDelivery);
        order.UpdateStatus(StoreOrderStatus.Delivered);
        order.Status.Should().Be(StoreOrderStatus.Delivered);
    }

    [Fact]
    public void UpdateStatus_SkipStep_Throws()
    {
        var order = MakeOrder();
        order.ReportPayment();
        order.Confirm();
        var act = () => order.UpdateStatus(StoreOrderStatus.Delivered); // skip Preparing
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void UpdateStatus_ReverseTransition_Throws()
    {
        var order = MakeOrder();
        order.ReportPayment();
        order.Confirm();
        order.UpdateStatus(StoreOrderStatus.Preparing);
        var act = () => order.UpdateStatus(StoreOrderStatus.Confirmed); // reversal
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Cancel_FromConfirmed_Transitions()
    {
        var order = MakeOrder();
        order.ReportPayment();
        order.Confirm();
        order.UpdateStatus(StoreOrderStatus.Cancelled);
        order.Status.Should().Be(StoreOrderStatus.Cancelled);
    }

    [Theory]
    [InlineData(StoreOrderStatus.Delivered)]
    [InlineData(StoreOrderStatus.Cancelled)]
    public void UpdateStatus_FromTerminalState_Throws(StoreOrderStatus from)
    {
        var order = MakeOrder();
        order.ReportPayment();
        order.Confirm();
        order.UpdateStatus(StoreOrderStatus.Preparing);
        order.UpdateStatus(from == StoreOrderStatus.Delivered
            ? StoreOrderStatus.ReadyForPickup
            : StoreOrderStatus.Cancelled);
        if (from == StoreOrderStatus.Delivered)
            order.UpdateStatus(StoreOrderStatus.Delivered);

        var act = () => order.UpdateStatus(StoreOrderStatus.Preparing);
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void TotalCrc_EqualsLineItemsSum()
    {
        var lines = new List<(Guid, string, int, decimal)>
        {
            (Guid.NewGuid(), "A", 3, 1000m),
            (Guid.NewGuid(), "B", 1, 2500m),
        };
        var order = StoreOrder.Place(Guid.NewGuid(), Guid.NewGuid(), "R", OrderFulfillmentType.Pickup, null, null, lines);
        order.TotalCrc.Should().Be(5500m);
    }
}

// ── PlaceStoreOrderCommandHandler tests ──────────────────────────────────────

public sealed class PlaceStoreOrderCommandHandlerTests
{
    private readonly IStoreRepository _storeRepo = Substitute.For<IStoreRepository>();
    private readonly IStoreOrderRepository _orderRepo = Substitute.For<IStoreOrderRepository>();
    private readonly IPaymentService _payment = Substitute.For<IPaymentService>();
    private readonly ISubscriptionService _subs = Substitute.For<ISubscriptionService>();
    private readonly INotificationDispatcher _notifications = Substitute.For<INotificationDispatcher>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly PlaceStoreOrderCommandHandler _sut;

    private static readonly Guid StoreOwnerId = Guid.NewGuid();
    private static readonly Guid StoreId = Guid.NewGuid();
    private static readonly Guid ProductId = Guid.NewGuid();

    public PlaceStoreOrderCommandHandlerTests()
    {
        _payment.GenerateReference().Returns("SINPE001");
        _uow.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);
        _subs.GetActiveUserTierAsync(StoreOwnerId, Arg.Any<CancellationToken>())
             .Returns(SubscriptionTier.StorePlus);

        _notifications.DispatchNewStoreOrderAsync(
            Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<decimal>(),
            Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        _sut = new PlaceStoreOrderCommandHandler(
            _storeRepo, _orderRepo, _payment, _subs, _notifications, _uow,
            NullLogger<PlaceStoreOrderCommandHandler>.Instance);
    }

    private void SetupActiveStore()
    {
        var store = Store.Create(StoreOwnerId, "Test Store", "Desc", "Addr", 9.9m, -84.0m, "store@test.com");
        typeof(Store).GetProperty("Id")!.SetValue(store, StoreId);
        typeof(Store).GetProperty("Status")!.SetValue(store, StoreStatus.Active);
        _storeRepo.GetByIdAsync(StoreId, Arg.Any<CancellationToken>()).Returns(store);
    }

    private void SetupAvailableProduct(decimal price = 2000m)
    {
        var product = StoreProduct.Create(StoreId, "Dog Food 3kg", null, ProductCategory.Food, price);
        typeof(StoreProduct).GetProperty("Id")!.SetValue(product, ProductId);
        _storeRepo.GetProductsByIdsAsync(
            Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, StoreProduct> { { ProductId, product } });
    }

    [Fact]
    public async Task Handle_ValidOrder_CreatesOrderAndReturnsDto()
    {
        SetupActiveStore();
        SetupAvailableProduct(2000m);

        var cmd = new PlaceStoreOrderCommand(
            CustomerId: Guid.NewGuid(),
            StoreId: StoreId,
            FulfillmentType: OrderFulfillmentType.Pickup,
            DeliveryAddress: null,
            CustomerNote: null,
            Lines: [new PlaceOrderLineInput(ProductId, 2)]);

        var result = await _sut.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCrc.Should().Be(4000m);
        result.Value.PaymentReference.Should().Be("SINPE001");
        await _orderRepo.Received(1).AddAsync(Arg.Any<StoreOrder>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_LocationBelongsToAnotherStore_ReturnsFailure()
    {
        SetupActiveStore();
        SetupAvailableProduct(2000m);

        var foreignLocation = StoreLocation.Create(Guid.NewGuid(), "Ajena", "Otra dir", 9.9m, -84m, null);
        _storeRepo.GetLocationByIdAsync(foreignLocation.Id, Arg.Any<CancellationToken>()).Returns(foreignLocation);

        var cmd = new PlaceStoreOrderCommand(
            CustomerId: Guid.NewGuid(),
            StoreId: StoreId,
            FulfillmentType: OrderFulfillmentType.Pickup,
            DeliveryAddress: null,
            CustomerNote: null,
            Lines: [new PlaceOrderLineInput(ProductId, 1)],
            LocationId: foreignLocation.Id);

        var result = await _sut.Handle(cmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        await _orderRepo.DidNotReceive().AddAsync(Arg.Any<StoreOrder>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ValidActiveLocation_AttributesOrderToLocation()
    {
        SetupActiveStore();
        SetupAvailableProduct(2000m);

        var location = StoreLocation.Create(StoreId, "Sucursal Norte", "Norte", 9.9m, -84m, null);
        _storeRepo.GetLocationByIdAsync(location.Id, Arg.Any<CancellationToken>()).Returns(location);

        var cmd = new PlaceStoreOrderCommand(
            CustomerId: Guid.NewGuid(),
            StoreId: StoreId,
            FulfillmentType: OrderFulfillmentType.Pickup,
            DeliveryAddress: null,
            CustomerNote: null,
            Lines: [new PlaceOrderLineInput(ProductId, 1)],
            LocationId: location.Id);

        var result = await _sut.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _orderRepo.Received(1).AddAsync(
            Arg.Is<StoreOrder>(o => o.LocationId == location.Id), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_StoreNotFound_ReturnsFailure()
    {
        _storeRepo.GetByIdAsync(StoreId, Arg.Any<CancellationToken>()).Returns((Store?)null);

        var cmd = new PlaceStoreOrderCommand(Guid.NewGuid(), StoreId, OrderFulfillmentType.Pickup,
            null, null, [new PlaceOrderLineInput(ProductId, 1)]);

        var result = await _sut.Handle(cmd, CancellationToken.None);
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_StorePlanGateFails_ReturnsFailure()
    {
        SetupActiveStore();
        _subs.GetActiveUserTierAsync(StoreOwnerId, Arg.Any<CancellationToken>())
             .Returns(SubscriptionTier.StoreBasic);

        var cmd = new PlaceStoreOrderCommand(Guid.NewGuid(), StoreId, OrderFulfillmentType.Pickup,
            null, null, [new PlaceOrderLineInput(ProductId, 1)]);

        var result = await _sut.Handle(cmd, CancellationToken.None);
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Contains("línea") || e.Contains("acepta"));
    }

    [Fact]
    public async Task Handle_DuplicateProductIds_ValidationFails()
    {
        var validator = new PlaceStoreOrderCommandValidator();
        var cmd = new PlaceStoreOrderCommand(Guid.NewGuid(), StoreId, OrderFulfillmentType.Pickup,
            null, null, [new PlaceOrderLineInput(ProductId, 1), new PlaceOrderLineInput(ProductId, 2)]);

        var result = await validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("mismo producto"));
    }

    [Fact]
    public async Task Handle_ExceedsMaxLines_ValidationFails()
    {
        var validator = new PlaceStoreOrderCommandValidator();
        var lines = Enumerable.Range(0, 21)
            .Select(_ => new PlaceOrderLineInput(Guid.NewGuid(), 1))
            .ToList();
        var cmd = new PlaceStoreOrderCommand(Guid.NewGuid(), StoreId, OrderFulfillmentType.Pickup,
            null, null, lines);

        var result = await validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_DeliveryWithoutAddress_ValidationFails()
    {
        var validator = new PlaceStoreOrderCommandValidator();
        var cmd = new PlaceStoreOrderCommand(Guid.NewGuid(), StoreId, OrderFulfillmentType.Delivery,
            DeliveryAddress: null, null, [new PlaceOrderLineInput(ProductId, 1)]);

        var result = await validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }
}
