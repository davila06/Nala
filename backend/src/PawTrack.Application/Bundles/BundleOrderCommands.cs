using FluentValidation;
using MediatR;
using PawTrack.Application.Bundles.Interfaces;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Subscriptions.Interfaces;
using PawTrack.Domain.Bundles;
using PawTrack.Domain.Common;
using PawTrack.Domain.Subscriptions;

namespace PawTrack.Application.Bundles;

// ── Price constants ───────────────────────────────────────────────────────────

public static class BundlePrices
{
    public const decimal BundleCrc = 49_900m;
    public const int SubscriptionMonths = 12;

    // ── Accessory-only pricing ────────────────────────────────────────────────
    public static decimal GetPrice(BundleProductType product) => product switch
    {
        BundleProductType.QrPlate => 4_500m,
        BundleProductType.SiliconeTag => 5_500m,
        BundleProductType.NfcQrCombo => 12_000m,
        BundleProductType.EmergencyPack => 7_000m,
        _ => BundleCrc, // CollarGpsPlus default
    };
}

// ── DTOs ─────────────────────────────────────────────────────────────────────

public sealed record BundleOrderDto(
    Guid Id,
    Guid UserId,
    string CollarModel,
    string CollarModelLabel,
    string ProductType,
    string ProductTypeLabel,
    string Status,
    string StatusLabel,
    string PaymentReference,
    decimal AmountCrc,
    string ShippingFullName,
    string ShippingAddress,
    string ShippingCanton,
    string ShippingPhone,
    string? DeliveryNotes,
    string? TrackingNumber,
    string? AdminNotes,
    bool PaymentReportedByUser,
    Guid? ActivatedSubscriptionId,
    DateTimeOffset CreatedAt,
    DateTimeOffset? PaidAt,
    DateTimeOffset? SourcedAt,
    DateTimeOffset? ShippedAt,
    DateTimeOffset? DeliveredAt,
    DateTimeOffset? CancelledAt)
{
    private static readonly Dictionary<Domain.Bundles.CollarModel, string> CollarLabels = new()
    {
        [Domain.Bundles.CollarModel.TractiveGPSDog4] = "Tractive GPS DOG 4",
        [Domain.Bundles.CollarModel.TractiveGPSCat4] = "Tractive GPS CAT 4",
    };

    private static readonly Dictionary<Domain.Bundles.BundleOrderStatus, string> StatusLabels = new()
    {
        [Domain.Bundles.BundleOrderStatus.PendingPayment] = "Pendiente de pago",
        [Domain.Bundles.BundleOrderStatus.Paid] = "Pago confirmado",
        [Domain.Bundles.BundleOrderStatus.Sourcing] = "Adquiriendo collar",
        [Domain.Bundles.BundleOrderStatus.Shipped] = "En camino",
        [Domain.Bundles.BundleOrderStatus.Delivered] = "Entregado",
        [Domain.Bundles.BundleOrderStatus.Cancelled] = "Cancelado",
    };

    private static readonly Dictionary<BundleProductType, string> ProductLabels = new()
    {
        [BundleProductType.CollarGpsPlus] = "Bundle Collar GPS + 12 meses Plus",
        [BundleProductType.QrPlate] = "Placa QR de aluminio",
        [BundleProductType.SiliconeTag] = "Tag de silicona con QR",
        [BundleProductType.NfcQrCombo] = "Combo NFC + QR",
        [BundleProductType.EmergencyPack] = "Pack emergencia (placa + tarjeta bolsillo)",
    };

    public static BundleOrderDto FromDomain(BundleOrder o) => new(
        o.Id, o.UserId,
        o.CollarModel.ToString(),
        CollarLabels.TryGetValue(o.CollarModel, out var cl) ? cl : o.CollarModel.ToString(),
        o.ProductType.ToString(),
        ProductLabels.TryGetValue(o.ProductType, out var pl) ? pl : o.ProductType.ToString(),
        o.Status.ToString(),
        StatusLabels.TryGetValue(o.Status, out var sl) ? sl : o.Status.ToString(),
        o.PaymentReference, o.AmountCrc,
        o.ShippingFullName, o.ShippingAddress, o.ShippingCanton, o.ShippingPhone,
        o.DeliveryNotes, o.TrackingNumber, o.AdminNotes,
        o.PaymentReportedByUser, o.ActivatedSubscriptionId,
        o.CreatedAt, o.PaidAt, o.SourcedAt, o.ShippedAt, o.DeliveredAt, o.CancelledAt);
}

public sealed record BundleOrderPageDto(IReadOnlyList<BundleOrderDto> Items, int Total, int Page, int PageSize);

// ── Create order ──────────────────────────────────────────────────────────────

public sealed record CreateBundleOrderCommand(
    Guid UserId,
    CollarModel CollarModel,
    string ShippingFullName,
    string ShippingAddress,
    string ShippingCanton,
    string ShippingPhone,
    string? DeliveryNotes,
    BundleProductType ProductType = BundleProductType.CollarGpsPlus) : IRequest<Result<BundleOrderDto>>;

public sealed class CreateBundleOrderCommandValidator : AbstractValidator<CreateBundleOrderCommand>
{
    public CreateBundleOrderCommandValidator()
    {
        RuleFor(x => x.ShippingFullName).NotEmpty().MaximumLength(120);
        RuleFor(x => x.ShippingAddress).NotEmpty().MaximumLength(300);
        RuleFor(x => x.ShippingCanton).NotEmpty().MaximumLength(80);
        RuleFor(x => x.ShippingPhone).NotEmpty().MaximumLength(20)
            .Matches(@"^\+?[\d\s\-]{7,20}$").WithMessage("Teléfono inválido.");
        RuleFor(x => x.DeliveryNotes).MaximumLength(300);
    }
}

public sealed class CreateBundleOrderCommandHandler(
    IBundleOrderRepository repository,
    IPaymentService paymentService,
    IEmailSender emailSender,
    IUserRepository userRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<CreateBundleOrderCommand, Result<BundleOrderDto>>
{
    public async Task<Result<BundleOrderDto>> Handle(
        CreateBundleOrderCommand request, CancellationToken ct)
    {
        var reference = paymentService.GenerateReference();
        var order = BundleOrder.Create(
            request.UserId, request.CollarModel, reference,
            BundlePrices.GetPrice(request.ProductType),
            request.ShippingFullName, request.ShippingAddress,
            request.ShippingCanton, request.ShippingPhone,
            request.DeliveryNotes,
            request.ProductType);

        await repository.AddAsync(order, ct);
        await unitOfWork.SaveChangesAsync(ct);

        var user = await userRepository.GetByIdAsync(request.UserId, ct);
        if (user is not null)
        {
            var dto = BundleOrderDto.FromDomain(order);
            _ = emailSender.SendBundleOrderConfirmationAsync(
                user.Email, user.Name,
                dto.ProductTypeLabel,
                reference, dto.AmountCrc,
                $"{request.ShippingAddress}, {request.ShippingCanton}", ct);
        }

        return Result.Success(BundleOrderDto.FromDomain(order));
    }
}

// ── User reports payment sent ─────────────────────────────────────────────────

public sealed record ReportBundlePaymentCommand(Guid OrderId, Guid UserId) : IRequest<Result<bool>>;

public sealed class ReportBundlePaymentCommandHandler(
    IBundleOrderRepository repository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<ReportBundlePaymentCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(ReportBundlePaymentCommand request, CancellationToken ct)
    {
        var order = await repository.GetByIdAsync(request.OrderId, ct);
        if (order is null || order.UserId != request.UserId)
            return Result.Failure<bool>("Pedido no encontrado.");
        if (order.Status != BundleOrderStatus.PendingPayment)
            return Result.Failure<bool>("El pedido no está en estado de pago pendiente.");

        order.ReportPaymentSent();
        repository.Update(order);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success(true);
    }
}

// ── Admin: confirm payment → activates 12-month Plus subscription ─────────────

public sealed record ConfirmBundlePaymentCommand(Guid OrderId) : IRequest<Result<BundleOrderDto>>;

public sealed class ConfirmBundlePaymentCommandHandler(
    IBundleOrderRepository bundleRepository,
    ISubscriptionRepository subscriptionRepository,
    IPaymentService paymentService,
    IEmailSender emailSender,
    IUserRepository userRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<ConfirmBundlePaymentCommand, Result<BundleOrderDto>>
{
    public async Task<Result<BundleOrderDto>> Handle(
        ConfirmBundlePaymentCommand request, CancellationToken ct)
    {
        var order = await bundleRepository.GetByIdAsync(request.OrderId, ct);
        if (order is null) return Result.Failure<BundleOrderDto>("Pedido no encontrado.");
        if (order.Status != BundleOrderStatus.PendingPayment)
            return Result.Failure<BundleOrderDto>("El pedido ya fue procesado.");

        // Create and immediately activate a 12-month UserPlus subscription
        var subRef = paymentService.GenerateReference();
        var subscription = Subscription.CreateForUser(
            order.UserId, SubscriptionTier.UserPlus, subRef, 0m);
        subscription.Activate(BundlePrices.SubscriptionMonths);

        await subscriptionRepository.AddAsync(subscription, ct);
        order.ConfirmPayment(subscription.Id);
        bundleRepository.Update(order);
        await unitOfWork.SaveChangesAsync(ct);

        var dto = BundleOrderDto.FromDomain(order);
        var user = await userRepository.GetByIdAsync(order.UserId, ct);
        if (user is not null)
            _ = emailSender.SendBundlePaymentConfirmedAsync(user.Email, user.Name, dto.CollarModelLabel, ct);

        return Result.Success(dto);
    }
}

// ── Admin: mark sourcing ──────────────────────────────────────────────────────

public sealed record MarkBundleSourcedCommand(Guid OrderId, string? AdminNotes) : IRequest<Result<BundleOrderDto>>;

public sealed class MarkBundleSourcedCommandHandler(IBundleOrderRepository repo, IUnitOfWork uow)
    : IRequestHandler<MarkBundleSourcedCommand, Result<BundleOrderDto>>
{
    public async Task<Result<BundleOrderDto>> Handle(MarkBundleSourcedCommand request, CancellationToken ct)
    {
        var order = await repo.GetByIdAsync(request.OrderId, ct);
        if (order is null) return Result.Failure<BundleOrderDto>("Pedido no encontrado.");
        if (order.Status != BundleOrderStatus.Paid)
            return Result.Failure<BundleOrderDto>("El pedido debe estar en estado Pagado para marcar como en adquisición.");

        order.MarkSourcing(request.AdminNotes);
        repo.Update(order);
        await uow.SaveChangesAsync(ct);
        return Result.Success(BundleOrderDto.FromDomain(order));
    }
}

// ── Admin: mark shipped ───────────────────────────────────────────────────────

public sealed record MarkBundleShippedCommand(Guid OrderId, string TrackingNumber, string? AdminNotes)
    : IRequest<Result<BundleOrderDto>>;

public sealed class MarkBundleShippedCommandHandler(
    IBundleOrderRepository repo,
    IEmailSender emailSender,
    IUserRepository userRepository,
    IUnitOfWork uow)
    : IRequestHandler<MarkBundleShippedCommand, Result<BundleOrderDto>>
{
    public async Task<Result<BundleOrderDto>> Handle(MarkBundleShippedCommand request, CancellationToken ct)
    {
        var order = await repo.GetByIdAsync(request.OrderId, ct);
        if (order is null) return Result.Failure<BundleOrderDto>("Pedido no encontrado.");
        if (order.Status is not (BundleOrderStatus.Paid or BundleOrderStatus.Sourcing))
            return Result.Failure<BundleOrderDto>("El pedido no puede marcarse como enviado en su estado actual.");

        order.MarkShipped(request.TrackingNumber, request.AdminNotes);
        repo.Update(order);
        await uow.SaveChangesAsync(ct);

        var dto = BundleOrderDto.FromDomain(order);
        var user = await userRepository.GetByIdAsync(order.UserId, ct);
        if (user is not null)
            _ = emailSender.SendBundleShippedAsync(user.Email, user.Name, dto.CollarModelLabel, request.TrackingNumber, ct);

        return Result.Success(dto);
    }
}

// ── Admin: mark delivered ─────────────────────────────────────────────────────

public sealed record MarkBundleDeliveredCommand(Guid OrderId) : IRequest<Result<BundleOrderDto>>;

public sealed class MarkBundleDeliveredCommandHandler(IBundleOrderRepository repo, IUnitOfWork uow)
    : IRequestHandler<MarkBundleDeliveredCommand, Result<BundleOrderDto>>
{
    public async Task<Result<BundleOrderDto>> Handle(MarkBundleDeliveredCommand request, CancellationToken ct)
    {
        var order = await repo.GetByIdAsync(request.OrderId, ct);
        if (order is null) return Result.Failure<BundleOrderDto>("Pedido no encontrado.");
        if (order.Status != BundleOrderStatus.Shipped)
            return Result.Failure<BundleOrderDto>("El pedido debe estar en estado Enviado.");

        order.MarkDelivered();
        repo.Update(order);
        await uow.SaveChangesAsync(ct);
        return Result.Success(BundleOrderDto.FromDomain(order));
    }
}

// ── Cancel (admin or user if PendingPayment) ──────────────────────────────────

public sealed record CancelBundleOrderCommand(
    Guid OrderId, Guid RequestingUserId, bool IsAdmin, string? AdminNotes)
    : IRequest<Result<BundleOrderDto>>;

public sealed class CancelBundleOrderCommandHandler(IBundleOrderRepository repo, IUnitOfWork uow)
    : IRequestHandler<CancelBundleOrderCommand, Result<BundleOrderDto>>
{
    public async Task<Result<BundleOrderDto>> Handle(CancelBundleOrderCommand request, CancellationToken ct)
    {
        var order = await repo.GetByIdAsync(request.OrderId, ct);
        if (order is null) return Result.Failure<BundleOrderDto>("Pedido no encontrado.");

        if (!request.IsAdmin)
        {
            if (order.UserId != request.RequestingUserId)
                return Result.Failure<BundleOrderDto>("Acceso denegado.");
            if (!order.CanBeCancelledByUser)
                return Result.Failure<BundleOrderDto>(
                    "Solo puedes cancelar un pedido pendiente de pago. Contacta a soporte para cancelaciones posteriores.");
        }

        order.Cancel(request.AdminNotes);
        repo.Update(order);
        await uow.SaveChangesAsync(ct);
        return Result.Success(BundleOrderDto.FromDomain(order));
    }
}

// ── Queries ───────────────────────────────────────────────────────────────────

public sealed record GetMyBundleOrdersQuery(Guid UserId) : IRequest<Result<IReadOnlyList<BundleOrderDto>>>;

public sealed class GetMyBundleOrdersQueryHandler(IBundleOrderRepository repo)
    : IRequestHandler<GetMyBundleOrdersQuery, Result<IReadOnlyList<BundleOrderDto>>>
{
    public async Task<Result<IReadOnlyList<BundleOrderDto>>> Handle(
        GetMyBundleOrdersQuery request, CancellationToken ct)
    {
        var orders = await repo.GetByUserIdAsync(request.UserId, ct);
        return Result.Success<IReadOnlyList<BundleOrderDto>>(
            orders.Select(BundleOrderDto.FromDomain).ToList());
    }
}

public sealed record GetAllBundleOrdersQuery(
    BundleOrderStatus? Status, int Page = 1, int PageSize = 25)
    : IRequest<Result<BundleOrderPageDto>>;

public sealed class GetAllBundleOrdersQueryHandler(IBundleOrderRepository repo)
    : IRequestHandler<GetAllBundleOrdersQuery, Result<BundleOrderPageDto>>
{
    public async Task<Result<BundleOrderPageDto>> Handle(
        GetAllBundleOrdersQuery request, CancellationToken ct)
    {
        var skip = (request.Page - 1) * request.PageSize;
        var items = await repo.GetAllPagedAsync(request.Status, skip, request.PageSize, ct);
        var total = await repo.CountAllAsync(request.Status, ct);

        return Result.Success(new BundleOrderPageDto(
            items.Select(BundleOrderDto.FromDomain).ToList(),
            total, request.Page, request.PageSize));
    }
}
