using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Stores;
using PawTrack.Application.Subscriptions.Interfaces;
using PawTrack.Application.Subscriptions.Services;
using PawTrack.Domain.Audit;
using PawTrack.Domain.Stores;
using PawTrack.Domain.Subscriptions;

namespace PawTrack.UnitTests.Stores;

// ── StoreOrder domain: state machine ─────────────────────────────────────────

public sealed class StoreOrderStateMachineTests
{
    private static StoreOrder MakeOrder(
        OrderFulfillmentType fulfillment = OrderFulfillmentType.Pickup,
        bool reserveInventory = true)
    {
        var lines = new List<(Guid, string, int, decimal)>
        {
            (Guid.NewGuid(), "Product A", 2, 1500m)
        };
        var order = StoreOrder.Place(Guid.NewGuid(), Guid.NewGuid(), "REF12345",
            fulfillment, null, null, lines);
        if (reserveInventory)
            order.MarkStockReserved(DateTimeOffset.UtcNow.AddMinutes(15));
        return order;
    }

    [Fact]
    public void NewOrder_AwaitsStoreAcceptanceBeforePayment()
    {
        var order = MakeOrder(reserveInventory: false);
        order.Status.ToString().Should().Be("AwaitingStoreAcceptance");
    }

    [Fact]
    public void StoreAcceptance_TransitionsToAwaitingPayment()
    {
        var order = MakeOrder();

        order.Accept("Disponibilidad confirmada");

        order.Status.ToString().Should().Be("AwaitingPayment");
    }

    [Fact]
    public void CustomerCanReportPaymentOnlyAfterStoreAcceptance()
    {
        var order = MakeOrder();
        order.Accept("Disponibilidad confirmada");

        order.ReportPayment();

        order.Status.Should().Be(StoreOrderStatus.PaymentReported);
    }

    [Fact]
    public void ReportPayment_FromAwaitingPayment_Transitions()
    {
        var order = MakeOrder();
        order.Accept("Disponibilidad confirmada");
        order.ReportPayment();
        order.Status.Should().Be(StoreOrderStatus.PaymentReported);
        order.PaymentReportedByCustomer.Should().BeTrue();
    }

    [Fact]
    public void ReportPayment_WhenAlreadyReported_Throws()
    {
        var order = MakeOrder();
        order.Accept("Disponibilidad confirmada");
        order.ReportPayment();
        var act = () => order.ReportPayment();
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void VerifyManualPayment_FromPaymentReported_RecordsVerifierAndReference()
    {
        var order = MakeOrder();
        var verifierId = Guid.NewGuid();
        order.Accept("Disponibilidad confirmada");
        order.ReportPayment();
        order.VerifyManualPayment(verifierId, "BANK-TX-1", "Looking good");
        order.Status.Should().Be(StoreOrderStatus.Paid);
        order.PaymentVerifiedByUserId.Should().Be(verifierId);
        order.PaymentVerificationReference.Should().Be("BANK-TX-1");
        order.PaymentConfirmedAt.Should().NotBeNull();
    }

    [Fact]
    public void Accept_FromInitialRequest_TransitionsWithoutPaymentReport()
    {
        var order = MakeOrder();

        order.Accept("Disponibilidad confirmada");

        order.Status.Should().Be(StoreOrderStatus.AwaitingPayment);
        order.PaymentReportedByCustomer.Should().BeFalse();
        order.StoreNote.Should().Be("Disponibilidad confirmada");
    }

    [Fact]
    public void Reject_FromInitialRequest_TransitionsToRejected()
    {
        var order = MakeOrder();

        order.Reject("Producto no disponible");

        order.Status.Should().Be(StoreOrderStatus.Rejected);
        order.StoreNote.Should().Be("Producto no disponible");
    }

    [Fact]
    public void Reject_WithoutReason_Throws()
    {
        var order = MakeOrder();

        var act = () => order.Reject(" ");

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Cancel_WithoutReason_Throws()
    {
        var order = MakeOrder();
        order.Accept();

        var act = () => order.UpdateStatus(StoreOrderStatus.Cancelled);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Cancel_WithReason_StoresReason()
    {
        var order = MakeOrder();
        order.Accept();

        order.UpdateStatus(StoreOrderStatus.Cancelled, "Cliente no disponible");

        order.Status.Should().Be(StoreOrderStatus.Cancelled);
        order.StoreNote.Should().Be("Cliente no disponible");
    }

    [Fact]
    public void UpdateStatus_ValidPickupPath_Transitions()
    {
        var order = MakeOrder(OrderFulfillmentType.Pickup);
        order.Accept("Disponibilidad confirmada");
        order.ReportPayment();
        order.VerifyManualPayment(Guid.NewGuid(), "BANK-PICKUP");
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
        order.Accept("Disponibilidad confirmada");
        order.ReportPayment();
        order.VerifyManualPayment(Guid.NewGuid(), "BANK-DELIVERY");
        order.UpdateStatus(StoreOrderStatus.Preparing);
        order.UpdateStatus(StoreOrderStatus.OutForDelivery);
        order.UpdateStatus(StoreOrderStatus.Delivered);
        order.Status.Should().Be(StoreOrderStatus.Delivered);
    }

    [Fact]
    public void UpdateStatus_SkipStep_Throws()
    {
        var order = MakeOrder();
        order.Accept("Disponibilidad confirmada");
        order.ReportPayment();
        order.VerifyManualPayment(Guid.NewGuid(), "BANK-SKIP");
        var act = () => order.UpdateStatus(StoreOrderStatus.Delivered); // skip Preparing
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void UpdateStatus_ReverseTransition_Throws()
    {
        var order = MakeOrder();
        order.Accept("Disponibilidad confirmada");
        order.ReportPayment();
        order.VerifyManualPayment(Guid.NewGuid(), "BANK-REVERSE");
        order.UpdateStatus(StoreOrderStatus.Preparing);
        var act = () => order.UpdateStatus(StoreOrderStatus.Confirmed); // reversal
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Cancel_FromPaid_Transitions()
    {
        var order = MakeOrder();
        order.Accept("Disponibilidad confirmada");
        order.ReportPayment();
        order.VerifyManualPayment(Guid.NewGuid(), "BANK-CANCEL");
        order.UpdateStatus(StoreOrderStatus.Cancelled, "Cancelado por la tienda");
        order.Status.Should().Be(StoreOrderStatus.Cancelled);
    }

    [Theory]
    [InlineData(StoreOrderStatus.Delivered)]
    [InlineData(StoreOrderStatus.Cancelled)]
    public void UpdateStatus_FromTerminalState_Throws(StoreOrderStatus from)
    {
        var order = MakeOrder();
        order.Accept("Disponibilidad confirmada");
        order.ReportPayment();
        order.VerifyManualPayment(Guid.NewGuid(), "BANK-TERMINAL");
        order.UpdateStatus(StoreOrderStatus.Preparing);
        order.UpdateStatus(from == StoreOrderStatus.Delivered
            ? StoreOrderStatus.ReadyForPickup
            : StoreOrderStatus.Cancelled, "Cancelado para probar estado terminal");
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

public sealed class StoreInventoryReservationTests
{
    [Fact]
    public void ProductReservationDecrementsAvailableStockAndReleaseRestoresItOnce()
    {
        var product = StoreProduct.Create(Guid.NewGuid(), "Alimento", null, ProductCategory.Food, 5000m, stockOnHand: 3);

        product.ReserveStock(2);
        product.StockOnHand.Should().Be(1);

        product.ReleaseStock(2);
        product.StockOnHand.Should().Be(3);
    }

    [Fact]
    public void ProductCannotReserveMoreThanAvailableStock()
    {
        var product = StoreProduct.Create(Guid.NewGuid(), "Alimento", null, ProductCategory.Food, 5000m, stockOnHand: 1);

        var act = () => product.ReserveStock(2);

        act.Should().Throw<InvalidOperationException>();
        product.StockOnHand.Should().Be(1);
    }

    [Fact]
    public void ManualPaymentVerificationConsumesReservationAndIsIdempotent()
    {
        var order = StoreOrder.Place(Guid.NewGuid(), Guid.NewGuid(), "REF54321", OrderFulfillmentType.Pickup,
            null, null, [(Guid.NewGuid(), "Alimento", 1, 5000m)]);
        order.MarkStockReserved(DateTimeOffset.UtcNow.AddMinutes(15));
        order.Accept("Disponibilidad confirmada");
        var verifierId = Guid.NewGuid();
        order.ReportPayment();

        order.VerifyManualPayment(verifierId, "BANK-REF-1");
        order.VerifyManualPayment(verifierId, "BANK-REF-1");

        order.Status.Should().Be(StoreOrderStatus.Paid);
        order.PaymentVerifiedByUserId.Should().Be(verifierId);
        order.PaymentVerificationReference.Should().Be("BANK-REF-1");
        order.StockReserved.Should().BeFalse();
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
        result.Value!.TotalCrc.Should().Be(4000m);
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

public sealed class StoreOrderAuthorizationTests
{
    private readonly IStoreRepository _storeRepo = Substitute.For<IStoreRepository>();
    private readonly IStoreOrderRepository _orderRepo = Substitute.For<IStoreOrderRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    [Fact]
    public async Task ConfirmOrder_FromDifferentStoreOwner_ReturnsNotFoundAndDoesNotMutate()
    {
        var realOwnerId = Guid.NewGuid();
        var attackerId = Guid.NewGuid();
        var store = Store.Create(realOwnerId, "Real Store", "Desc", "Address", 9.9m, -84m, "store@example.com");
        var order = StoreOrder.Place(store.Id, Guid.NewGuid(), "REF12345", OrderFulfillmentType.Pickup,
            null, null, [(Guid.NewGuid(), "Food", 1, 1000m)]);
        _storeRepo.GetByUserIdAsync(attackerId, Arg.Any<CancellationToken>()).Returns((Store?)null);

        var handler = new ConfirmStoreOrderCommandHandler(_storeRepo, _orderRepo);

        var result = await handler.Handle(
            new ConfirmStoreOrderCommand(attackerId, order.Id, "forged"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        _orderRepo.DidNotReceive().Update(Arg.Any<StoreOrder>());
        await _uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StoreAcceptance_RequiresAtomicInventoryReservation()
    {
        var ownerId = Guid.NewGuid();
        var store = Store.Create(ownerId, "Store", "Desc", "Address", 9.9m, -84m, "store@example.cr");
        var order = StoreOrder.Place(store.Id, Guid.NewGuid(), "REF54321", OrderFulfillmentType.Pickup,
            null, null, [(Guid.NewGuid(), "Food", 1, 1000m)]);
        _storeRepo.GetByUserIdAsync(ownerId, Arg.Any<CancellationToken>()).Returns(store);
        _orderRepo.GetByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);
        _orderRepo.TryAcceptAndReserveStockAsync(
            order.Id, store.Id, "Disponibilidad confirmada", Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var handler = new ConfirmStoreOrderCommandHandler(_storeRepo, _orderRepo);
        var result = await handler.Handle(new ConfirmStoreOrderCommand(ownerId, order.Id, "Disponibilidad confirmada"), default);

        result.IsFailure.Should().BeTrue();
        order.Status.Should().Be(StoreOrderStatus.AwaitingStoreAcceptance);
        await _orderRepo.Received(1).TryAcceptAndReserveStockAsync(
            order.Id, store.Id, "Disponibilidad confirmada", Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>());
        _orderRepo.DidNotReceive().Update(Arg.Any<StoreOrder>());
        await _uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetMyOrders_DoesNotReturnAnotherCustomersOrders()
    {
        var customerId = Guid.NewGuid();
        var foreignOrder = StoreOrder.Place(Guid.NewGuid(), Guid.NewGuid(), "REF54321",
            OrderFulfillmentType.Pickup, null, "private", [(Guid.NewGuid(), "Food", 1, 1000m)]);
        _orderRepo.CountByCustomerAsync(customerId, Arg.Any<CancellationToken>()).Returns(0);
        _orderRepo.GetByCustomerPagedAsync(customerId, 0, 20, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<StoreOrder>());

        var handler = new GetMyStoreOrdersQueryHandler(_orderRepo, _storeRepo);

        var result = await handler.Handle(new GetMyStoreOrdersQuery(customerId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().NotContain(item => item.Id == foreignOrder.Id);
        await _orderRepo.Received(1).GetByCustomerPagedAsync(customerId, 0, 20, Arg.Any<CancellationToken>());
    }
}

public sealed class StoreOrderPaymentVerificationTests
{
    [Fact]
    public async Task StoreOwnerVerifiesReportedPaymentAndAuditsReference()
    {
        var storeOwnerId = Guid.NewGuid();
        var store = Store.Create(storeOwnerId, "Store", "Desc", "Address", 9.9m, -84m, "store@example.cr");
        var order = StoreOrder.Place(store.Id, Guid.NewGuid(), "SINPE123", OrderFulfillmentType.Pickup,
            null, null, [(Guid.NewGuid(), "Food", 1, 1000m)]);
        order.MarkStockReserved(DateTimeOffset.UtcNow.AddMinutes(15));
        order.Accept("Disponible");
        order.ReportPayment();
        var storeRepository = Substitute.For<IStoreRepository>();
        var orderRepository = Substitute.For<IStoreOrderRepository>();
        var auditRepository = Substitute.For<IAuditLogRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        storeRepository.GetByUserIdAsync(storeOwnerId, Arg.Any<CancellationToken>()).Returns(store);
        orderRepository.GetByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);
        var handler = new VerifyStoreOrderPaymentCommandHandler(storeRepository, orderRepository, auditRepository, unitOfWork);

        var result = await handler.Handle(
            new VerifyStoreOrderPaymentCommand(storeOwnerId, order.Id, "BANK-REF-998", "SINPE recibido"), default);

        result.IsSuccess.Should().BeTrue();
        order.Status.Should().Be(StoreOrderStatus.Paid);
        order.PaymentVerifiedByUserId.Should().Be(storeOwnerId);
        order.PaymentVerificationReference.Should().Be("BANK-REF-998");
        await auditRepository.Received(1).AddAsync(
            Arg.Is<AuditLogEntry>(entry => entry.Action == AuditAction.StoreOrderPaymentVerified && entry.Details == "BANK-REF-998"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AnotherStoreCannotVerifyOrderPayment()
    {
        var order = StoreOrder.Place(Guid.NewGuid(), Guid.NewGuid(), "SINPE124", OrderFulfillmentType.Pickup,
            null, null, [(Guid.NewGuid(), "Food", 1, 1000m)]);
        var storeRepository = Substitute.For<IStoreRepository>();
        var orderRepository = Substitute.For<IStoreOrderRepository>();
        var auditRepository = Substitute.For<IAuditLogRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        storeRepository.GetByUserIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Store?)null);
        orderRepository.GetByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);
        var handler = new VerifyStoreOrderPaymentCommandHandler(storeRepository, orderRepository, auditRepository, unitOfWork);

        var result = await handler.Handle(
            new VerifyStoreOrderPaymentCommand(Guid.NewGuid(), order.Id, "BANK-REF-999", null), default);

        result.IsFailure.Should().BeTrue();
        order.Status.Should().Be(StoreOrderStatus.AwaitingStoreAcceptance);
        await auditRepository.DidNotReceive().AddAsync(Arg.Any<AuditLogEntry>(), Arg.Any<CancellationToken>());
    }
}
