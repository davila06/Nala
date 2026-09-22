using MediatR;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Subscriptions.Services;
using PawTrack.Domain.Common;
using PawTrack.Domain.ServiceProviders;

namespace PawTrack.Application.ServiceProviders;

public sealed record ProviderAnalyticsDto(
    Guid ServiceProviderId,
    DateTimeOffset From,
    DateTimeOffset To,
    int TotalBookings,
    int CompletedBookings,
    int CancelledBookings,
    decimal GrossRevenueCrc,
    decimal AverageBookingValueCrc,
    int PublishedServices,
    int ActiveScheduleBlocks);

public sealed record GetProviderAnalyticsQuery(
    Guid OwnerUserId,
    DateTimeOffset From,
    DateTimeOffset To) : IRequest<Result<ProviderAnalyticsDto>>;

public sealed class GetProviderAnalyticsQueryHandler(
    IServiceProviderRepository repository,
    IEntitlementService? entitlementService = null)
    : IRequestHandler<GetProviderAnalyticsQuery, Result<ProviderAnalyticsDto>>
{
    public async Task<Result<ProviderAnalyticsDto>> Handle(
        GetProviderAnalyticsQuery request,
        CancellationToken cancellationToken)
    {
        if (request.To <= request.From || request.To > request.From.AddDays(366))
            return Result.Failure<ProviderAnalyticsDto>("El rango debe ser válido y no superar 366 días.");

        var provider = await repository.GetByUserIdAsync(request.OwnerUserId, cancellationToken);
        if (provider is null)
            return Result.Failure<ProviderAnalyticsDto>("Proveedor no encontrado.");

        var retentionDays = provider.MembershipTier switch
        {
            ProviderMembershipTier.Verified => 90,
            ProviderMembershipTier.Featured => 730,
            _ => 0,
        };
        if (retentionDays == 0)
            return Result.Failure<ProviderAnalyticsDto>("La analítica requiere una membresía Verificada o Featured.");
        if (request.From < DateTimeOffset.UtcNow.AddDays(-retentionDays))
            return Result.Failure<ProviderAnalyticsDto>("El periodo excede la retención de analítica de la membresía.");

        if (entitlementService is not null)
        {
            var decision = await entitlementService.AuthorizeAsync(
                request.OwnerUserId, "AdvancedProviderAnalyticsEnabled", 1m,
                new EntitlementContext("provider-analytics", provider.Id), cancellationToken);
            if (decision.Included && !decision.Allowed)
                return Result.Failure<ProviderAnalyticsDto>("La analítica avanzada no está incluida en la membresía.");
        }

        const int pageSize = 5_000;
        var totalBookings = 0;
        var completed = 0;
        var cancelled = 0;
        var revenue = 0m;
        var skip = 0;

        while (true)
        {
            var bookings = await repository.GetBookingsByProviderAsync(provider.Id, skip, pageSize, cancellationToken);
            foreach (var booking in bookings.Where(booking => booking.CreatedAt >= request.From && booking.CreatedAt < request.To))
            {
                totalBookings++;
                if (booking.Status == ProviderBookingStatus.Completed)
                {
                    completed++;
                    revenue += booking.TotalCrc;
                }
                else if (booking.Status is ProviderBookingStatus.CancelledByCustomer or ProviderBookingStatus.CancelledByProvider)
                {
                    cancelled++;
                }
            }

            if (bookings.Count < pageSize)
                break;

            skip += pageSize;
        }

        var services = await repository.GetPublishedServicesByProviderAsync(provider.Id, cancellationToken);

        return Result.Success(new ProviderAnalyticsDto(
            provider.Id, request.From, request.To, totalBookings, completed, cancelled,
            revenue, completed == 0 ? 0m : Math.Round(revenue / completed, 2),
            services.Count, 0));
    }
}
