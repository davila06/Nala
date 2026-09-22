using System.Text;
using MediatR;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Audit;
using PawTrack.Domain.Common;
using PawTrack.Application.Subscriptions.Services;

namespace PawTrack.Application.Stores;

public sealed record ExportStoreAnalyticsCommand(
    Guid StoreOwnerUserId, int Year, int Month, Guid? LocationId = null)
    : IRequest<Result<byte[]>>;

public sealed class ExportStoreAnalyticsCommandHandler(
    ISender sender,
    IAuditLogRepository auditLogRepository,
    IUnitOfWork unitOfWork,
    IEntitlementService? entitlementService = null)
    : IRequestHandler<ExportStoreAnalyticsCommand, Result<byte[]>>
{
    public async Task<Result<byte[]>> Handle(ExportStoreAnalyticsCommand request, CancellationToken ct)
    {
        var monthStart = new DateTimeOffset(DateTimeOffset.UtcNow.Year, DateTimeOffset.UtcNow.Month, 1, 0, 0, 0, TimeSpan.Zero);
        var analyticsLimit = 20m;
        if (entitlementService is not null)
        {
            var decision = await entitlementService.AuthorizeAsync(
                request.StoreOwnerUserId, "MaxAnalyticsExportsPerCycle", 1m,
                new EntitlementContext("store-analytics-export", request.StoreOwnerUserId), ct);
            if (!decision.Allowed)
                return Result.Failure<byte[]>("La tienda alcanzó el límite de exportaciones analíticas.");
            if (decision.Limit.HasValue) analyticsLimit = decision.Limit.Value;
        }
        if (entitlementService is null && await auditLogRepository.CountByActionSinceAsync(
                AuditAction.StoreAnalyticsExported, request.StoreOwnerUserId, monthStart, ct) >= analyticsLimit)
            return Result.Failure<byte[]>("La tienda alcanzó el límite mensual de exportaciones.");

        var result = await sender.Send(new GetStoreAnalyticsQuery(
            request.StoreOwnerUserId, request.Year, request.Month, request.LocationId), ct);
        if (result.IsFailure) return Result.Failure<byte[]>(result.Errors);
        var analytics = result.Value!;
        var csv = new StringBuilder("metric,value\n");
        csv.AppendLine($"year,{analytics.Year}");
        csv.AppendLine($"month,{analytics.Month}");
        csv.AppendLine($"total_orders,{analytics.TotalOrders}");
        csv.AppendLine($"delivered_orders,{analytics.DeliveredOrders}");
        csv.AppendLine($"cancelled_orders,{analytics.CancelledOrders}");
        csv.AppendLine($"total_revenue_crc,{analytics.TotalRevenueCrc.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
        csv.AppendLine($"average_order_value_crc,{analytics.AverageOrderValueCrc.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
        foreach (var day in analytics.ByDay ?? [])
            csv.AppendLine($"day:{day.Day},orders:{day.OrderCount},revenue_crc:{day.RevenueCrc.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
        foreach (var product in analytics.TopProducts ?? [])
            csv.AppendLine($"product:{Escape(product.ProductName)},quantity:{product.QuantitySold},revenue_crc:{product.RevenueCrc.ToString(System.Globalization.CultureInfo.InvariantCulture)}");

        if (entitlementService is not null)
        {
            var consumption = await entitlementService.ConsumeAsync(
                request.StoreOwnerUserId, "MaxAnalyticsExportsPerCycle", 1m,
                $"store-analytics-export:{request.StoreOwnerUserId:N}:{request.Year}:{request.Month}:{request.LocationId}",
                new EntitlementContext("store-analytics-export", request.StoreOwnerUserId), ct);
            if (!consumption.Consumed)
                return Result.Failure<byte[]>("La tienda alcanzó el límite de exportaciones analíticas.");
        }

        await auditLogRepository.AddAsync(AuditLogEntry.Create(
            request.StoreOwnerUserId, AuditAction.StoreAnalyticsExported, "StoreAnalytics",
            request.StoreOwnerUserId.ToString(), $"{request.Year}-{request.Month:00}:{request.LocationId}"), ct);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success(Encoding.UTF8.GetBytes(csv.ToString()));
    }

    private static string Escape(string value) => value.Replace(",", " ", StringComparison.Ordinal).Replace("\n", " ", StringComparison.Ordinal);
}
