using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Subscriptions.Interfaces;
using PawTrack.Application.Subscriptions.Services;
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
    string StoreName,
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
    public static StoreOrderDto FromDomain(StoreOrder o, string storeName = "") => new(
        o.Id, o.StoreId, storeName, o.CustomerId,
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
        RuleFor(x => x.Lines).NotEmpty().WithMessage("El pedido debe tener al menos un producto.")
            .Must(l => l.Count <= 20).WithMessage("Un pedido puede tener máximo 20 líneas.")
            .Must(l => l.Select(x => x.ProductId).Distinct().Count() == l.Count)
            .WithMessage("No se puede repetir el mismo producto en múltiples líneas.");
        RuleForEach(x => x.Lines).ChildRules(l =>
        {
            l.RuleFor(x => x.Quantity).GreaterThan(0).LessThanOrEqualTo(100)
                .WithMessage("La cantidad por producto debe ser entre 1 y 100.");
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
    ISubscriptionService subscriptionService,
    INotificationDispatcher notificationDispatcher,
    IUnitOfWork uow,
    ILogger<PlaceStoreOrderCommandHandler> logger)
    : IRequestHandler<PlaceStoreOrderCommand, Result<StoreOrderDto>>
{
    public async Task<Result<StoreOrderDto>> Handle(PlaceStoreOrderCommand request, CancellationToken ct)
    {
        var store = await storeRepo.GetByIdAsync(request.StoreId, ct);
        if (store is null || store.Status != StoreStatus.Active)
            return Result.Failure<StoreOrderDto>("Tienda no disponible.");

        // Plan gate: only StorePlus+ stores can receive in-app orders
        var tier = await subscriptionService.GetActiveUserTierAsync(store.UserId, ct);
        if (tier is not (Domain.Subscriptions.SubscriptionTier.StorePlus or Domain.Subscriptions.SubscriptionTier.StorePartner))
            return Result.Failure<StoreOrderDto>("Esta tienda aún no acepta pedidos en línea. Contáctalos directamente.");

        // Batch-load all requested products in one query — avoids N+1
        var requestedIds = request.Lines.Select(l => l.ProductId).Distinct();
        var productMap = await storeRepo.GetProductsByIdsAsync(requestedIds, ct);

        var lines = new List<(Guid, string, int, decimal)>(request.Lines.Count);
        foreach (var line in request.Lines)
        {
            if (!productMap.TryGetValue(line.ProductId, out var product)
                || product.StoreId != store.Id
                || !product.IsAvailable)
                return Result.Failure<StoreOrderDto>($"Producto no disponible: {line.ProductId}");
            lines.Add((product.Id, product.Name, line.Quantity, product.PriceCrc));
        }

        var reference = paymentService.GenerateReference();
        var order = StoreOrder.Place(
            store.Id, request.CustomerId, reference,
            request.FulfillmentType, request.DeliveryAddress,
            request.CustomerNote, lines);

        await orderRepo.AddAsync(order, ct);

        try { await uow.SaveChangesAsync(ct); }
        catch (Exception ex)
            when (ex.Message.Contains("PaymentReference", StringComparison.OrdinalIgnoreCase)
               || (ex.InnerException?.Message.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase) == true
                   && ex.InnerException?.Message.Contains("PaymentReference", StringComparison.OrdinalIgnoreCase) == true))
        {
            // Extremely rare: two concurrent orders generated the same 8-char reference
            return Result.Failure<StoreOrderDto>("Error al generar referencia de pago. Por favor, intenta de nuevo.");
        }

        // Fire-and-forget push notification — uses None so it outlives the request's ct
        _ = notificationDispatcher.DispatchNewStoreOrderAsync(
            store.UserId, store.Name, order.Id.ToString(), order.TotalCrc, CancellationToken.None)
            .ContinueWith(t => logger.LogWarning(t.Exception,
                "StoreOrder push notification failed for order {OrderId}", order.Id),
                CancellationToken.None, TaskContinuationOptions.OnlyOnFaulted, TaskScheduler.Default);

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

        try { order.ReportPayment(); }
        catch (InvalidOperationException ex)
        { return Result.Failure<Unit>(ex.Message); }

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

        try { order.Confirm(request.Note); }
        catch (InvalidOperationException ex)
        { return Result.Failure<StoreOrderDto>(ex.Message); }

        orderRepo.Update(order);
        await uow.SaveChangesAsync(ct);
        return Result.Success(StoreOrderDto.FromDomain(order, store.Name));
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

        try { order.UpdateStatus(request.NewStatus, request.Note); }
        catch (InvalidOperationException ex)
        { return Result.Failure<StoreOrderDto>(ex.Message); }

        orderRepo.Update(order);
        await uow.SaveChangesAsync(ct);
        return Result.Success(StoreOrderDto.FromDomain(order, store.Name));
    }
}

// ── Get my orders (customer) ──────────────────────────────────────────────────

public sealed record GetMyStoreOrdersQuery(Guid CustomerId) : IRequest<Result<IReadOnlyList<StoreOrderDto>>>;

public sealed class GetMyStoreOrdersQueryHandler(IStoreOrderRepository repo, IStoreRepository storeRepo)
    : IRequestHandler<GetMyStoreOrdersQuery, Result<IReadOnlyList<StoreOrderDto>>>
{
    public async Task<Result<IReadOnlyList<StoreOrderDto>>> Handle(GetMyStoreOrdersQuery request, CancellationToken ct)
    {
        var orders = await repo.GetByCustomerAsync(request.CustomerId, ct);
        var storeIds = orders.Select(o => o.StoreId).Distinct();
        var storeNames = await storeRepo.GetStoreNamesByIdsAsync(storeIds, ct);
        return Result.Success<IReadOnlyList<StoreOrderDto>>(
            orders.Select(o => StoreOrderDto.FromDomain(o,
                storeNames.GetValueOrDefault(o.StoreId, "Tienda eliminada"))).ToList());
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

        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 50);
        var orders = await orderRepo.GetByStoreAsync(store.Id, page, pageSize, ct);
        return Result.Success<IReadOnlyList<StoreOrderDto>>(orders.Select(o => StoreOrderDto.FromDomain(o, store.Name)).ToList());
    }
}
