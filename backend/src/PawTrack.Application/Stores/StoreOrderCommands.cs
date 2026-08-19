using FluentValidation;
using MediatR;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Subscriptions.Interfaces;
using PawTrack.Domain.Common;
using PawTrack.Domain.Stores;

namespace PawTrack.Application.Stores;

// ── DTOs ──────────────────────────────────────────────────────────────────────

public sealed record StoreOrderItemDto(
    Guid Id,
    Guid ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPriceCrc,
    decimal SubtotalCrc)
{
    public static StoreOrderItemDto FromDomain(StoreOrderItem i) => new(
        i.Id, i.ProductId, i.ProductName, i.Quantity, i.UnitPriceCrc, i.SubtotalCrc);
}

public sealed record StoreOrderDto(
    Guid Id,
    Guid StoreId,
    Guid CustomerId,
    string Status,
    string FulfillmentType,
    string PaymentReference,
    decimal TotalCrc,
    string? DeliveryAddress,
    string? CustomerNote,
    string? StoreNote,
    bool PaymentReportedByCustomer,
    DateTimeOffset PlacedAt,
    DateTimeOffset? ConfirmedAt,
    DateTimeOffset? CompletedAt,
    IReadOnlyList<StoreOrderItemDto> Items)
{
    public static StoreOrderDto FromDomain(StoreOrder o) => new(
        o.Id, o.StoreId, o.CustomerId,
        o.Status.ToString(), o.FulfillmentType.ToString(),
        o.PaymentReference, o.TotalCrc,
        o.DeliveryAddress, o.CustomerNote, o.StoreNote,
        o.PaymentReportedByCustomer,
        o.PlacedAt, o.ConfirmedAt, o.CompletedAt,
        o.Items.Select(StoreOrderItemDto.FromDomain).ToList());
}

// ── Place order ───────────────────────────────────────────────────────────────

public sealed record PlaceOrderLineInput(Guid ProductId, int Quantity);

public sealed record PlaceStoreOrderCommand(
    Guid CustomerId,
    Guid StoreId,
    OrderFulfillmentType FulfillmentType,
    string? DeliveryAddress,
    string? CustomerNote,
    IReadOnlyList<PlaceOrderLineInput> Lines) : IRequest<Result<StoreOrderDto>>;

public sealed class PlaceStoreOrderCommandValidator : AbstractValidator<PlaceStoreOrderCommand>
{
    public PlaceStoreOrderCommandValidator()
    {
        RuleFor(x => x.StoreId).NotEmpty();
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.Lines).NotEmpty().WithMessage("El pedido debe tener al menos un producto.");
        RuleForEach(x => x.Lines).ChildRules(l =>
        {
            l.RuleFor(x => x.Quantity).GreaterThan(0);
        });
        RuleFor(x => x.DeliveryAddress)
            .NotEmpty()
            .When(x => x.FulfillmentType == OrderFulfillmentType.Delivery)
            .WithMessage("La dirección de entrega es requerida para pedidos a domicilio.");
    }
}

public sealed class PlaceStoreOrderCommandHandler(
    IStoreRepository storeRepo,
    IStoreOrderRepository orderRepo,
    IPaymentService paymentService,
    INotificationDispatcher notificationDispatcher,
    IUnitOfWork uow)
    : IRequestHandler<PlaceStoreOrderCommand, Result<StoreOrderDto>>
{
    public async Task<Result<StoreOrderDto>> Handle(PlaceStoreOrderCommand request, CancellationToken ct)
    {
        var store = await storeRepo.GetByIdAsync(request.StoreId, ct);
        if (store is null || store.Status != StoreStatus.Active)
            return Result.Failure<StoreOrderDto>("Tienda no disponible.");

        // Resolve products + validate availability
        var lines = new List<(Guid, string, int, decimal)>();
        foreach (var line in request.Lines)
        {
            var product = await storeRepo.GetProductByIdAsync(line.ProductId, ct);
            if (product is null || product.StoreId != store.Id || !product.IsAvailable)
                return Result.Failure<StoreOrderDto>($"Producto no disponible: {line.ProductId}");
            lines.Add((product.Id, product.Name, line.Quantity, product.PriceCrc));
        }

        var reference = paymentService.GenerateReference();
        var order = StoreOrder.Place(
            store.Id, request.CustomerId, reference,
            request.FulfillmentType, request.DeliveryAddress,
            request.CustomerNote, lines);

        await orderRepo.AddAsync(order, ct);
        await uow.SaveChangesAsync(ct);

        // Notify store owner of new order
        _ = notificationDispatcher.DispatchNewStoreOrderAsync(
            store.UserId, store.Name, order.Id.ToString(), order.TotalCrc, ct);

        return Result.Success(StoreOrderDto.FromDomain(order));
    }
}

// ── Report payment ────────────────────────────────────────────────────────────

public sealed record ReportStoreOrderPaymentCommand(Guid CustomerId, Guid OrderId) : IRequest<Result<Unit>>;

public sealed class ReportStoreOrderPaymentCommandHandler(IStoreOrderRepository repo, IUnitOfWork uow)
    : IRequestHandler<ReportStoreOrderPaymentCommand, Result<Unit>>
{
    public async Task<Result<Unit>> Handle(ReportStoreOrderPaymentCommand request, CancellationToken ct)
    {
        var order = await repo.GetByIdAsync(request.OrderId, ct);
        if (order is null || order.CustomerId != request.CustomerId)
            return Result.Failure<Unit>("Pedido no encontrado.");
        if (order.Status != StoreOrderStatus.PendingPayment)
            return Result.Failure<Unit>("El estado del pedido no permite esta acción.");

        order.ReportPayment();
        repo.Update(order);
        await uow.SaveChangesAsync(ct);
        return Result.Success(Unit.Value);
    }
}

// ── Confirm order (store owner) ───────────────────────────────────────────────

public sealed record ConfirmStoreOrderCommand(Guid StoreOwnerUserId, Guid OrderId, string? Note) : IRequest<Result<StoreOrderDto>>;

public sealed class ConfirmStoreOrderCommandHandler(
    IStoreRepository storeRepo,
    IStoreOrderRepository orderRepo,
    IUnitOfWork uow)
    : IRequestHandler<ConfirmStoreOrderCommand, Result<StoreOrderDto>>
{
    public async Task<Result<StoreOrderDto>> Handle(ConfirmStoreOrderCommand request, CancellationToken ct)
    {
        var store = await storeRepo.GetByUserIdAsync(request.StoreOwnerUserId, ct);
        if (store is null) return Result.Failure<StoreOrderDto>("Tienda no encontrada.");

        var order = await orderRepo.GetByIdAsync(request.OrderId, ct);
        if (order is null || order.StoreId != store.Id)
            return Result.Failure<StoreOrderDto>("Pedido no encontrado.");
        if (order.Status != StoreOrderStatus.PaymentReported)
            return Result.Failure<StoreOrderDto>("Solo se pueden confirmar pedidos con pago reportado.");

        order.Confirm(request.Note);
        orderRepo.Update(order);
        await uow.SaveChangesAsync(ct);
        return Result.Success(StoreOrderDto.FromDomain(order));
    }
}

// ── Update order status (store owner) ────────────────────────────────────────

public sealed record UpdateStoreOrderStatusCommand(
    Guid StoreOwnerUserId,
    Guid OrderId,
    StoreOrderStatus NewStatus,
    string? Note) : IRequest<Result<StoreOrderDto>>;

public sealed class UpdateStoreOrderStatusCommandHandler(
    IStoreRepository storeRepo,
    IStoreOrderRepository orderRepo,
    IUnitOfWork uow)
    : IRequestHandler<UpdateStoreOrderStatusCommand, Result<StoreOrderDto>>
{
    public async Task<Result<StoreOrderDto>> Handle(UpdateStoreOrderStatusCommand request, CancellationToken ct)
    {
        var store = await storeRepo.GetByUserIdAsync(request.StoreOwnerUserId, ct);
        if (store is null) return Result.Failure<StoreOrderDto>("Tienda no encontrada.");

        var order = await orderRepo.GetByIdAsync(request.OrderId, ct);
        if (order is null || order.StoreId != store.Id)
            return Result.Failure<StoreOrderDto>("Pedido no encontrado.");

        order.UpdateStatus(request.NewStatus, request.Note);
        orderRepo.Update(order);
        await uow.SaveChangesAsync(ct);
        return Result.Success(StoreOrderDto.FromDomain(order));
    }
}

// ── Get my orders (customer) ──────────────────────────────────────────────────

public sealed record GetMyStoreOrdersQuery(Guid CustomerId) : IRequest<Result<IReadOnlyList<StoreOrderDto>>>;

public sealed class GetMyStoreOrdersQueryHandler(IStoreOrderRepository repo)
    : IRequestHandler<GetMyStoreOrdersQuery, Result<IReadOnlyList<StoreOrderDto>>>
{
    public async Task<Result<IReadOnlyList<StoreOrderDto>>> Handle(GetMyStoreOrdersQuery request, CancellationToken ct)
    {
        var orders = await repo.GetByCustomerAsync(request.CustomerId, ct);
        return Result.Success<IReadOnlyList<StoreOrderDto>>(orders.Select(StoreOrderDto.FromDomain).ToList());
    }
}

// ── Get store orders (owner) ──────────────────────────────────────────────────

public sealed record GetStoreOrdersQuery(Guid StoreOwnerUserId, int Page = 1, int PageSize = 20)
    : IRequest<Result<IReadOnlyList<StoreOrderDto>>>;

public sealed class GetStoreOrdersQueryHandler(IStoreRepository storeRepo, IStoreOrderRepository orderRepo)
    : IRequestHandler<GetStoreOrdersQuery, Result<IReadOnlyList<StoreOrderDto>>>
{
    public async Task<Result<IReadOnlyList<StoreOrderDto>>> Handle(GetStoreOrdersQuery request, CancellationToken ct)
    {
        var store = await storeRepo.GetByUserIdAsync(request.StoreOwnerUserId, ct);
        if (store is null) return Result.Failure<IReadOnlyList<StoreOrderDto>>("Tienda no encontrada.");

        var orders = await orderRepo.GetByStoreAsync(store.Id, request.Page, request.PageSize, ct);
        return Result.Success<IReadOnlyList<StoreOrderDto>>(orders.Select(StoreOrderDto.FromDomain).ToList());
    }
}
