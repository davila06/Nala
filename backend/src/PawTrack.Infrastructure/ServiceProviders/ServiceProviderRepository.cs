using Microsoft.EntityFrameworkCore;
using System.Data;
using PawTrack.Application.Common;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.ServiceProviders;
using PawTrack.Infrastructure.Persistence;

namespace PawTrack.Infrastructure.ServiceProviders;

public sealed class ServiceProviderRepository(PawTrackDbContext db) : IServiceProviderRepository
{
    public Task<ServiceProvider?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.ServiceProviders.AsNoTracking().FirstOrDefaultAsync(provider => provider.Id == id, ct);

    public Task<ServiceProvider?> GetByUserIdAsync(Guid userId, CancellationToken ct = default) =>
        db.ServiceProviders.AsNoTracking().FirstOrDefaultAsync(provider => provider.UserId == userId, ct);

    public async Task<IReadOnlyList<ServiceProvider>> GetActivePagedAsync(
        ServiceProviderCategory? category,
        ServiceModality? modality,
        decimal? minPriceCrc,
        decimal? maxPriceCrc,
        int skip,
        int take,
        CancellationToken ct = default) =>
        await db.ServiceProviders.AsNoTracking()
            .Where(provider => provider.Status == ServiceProviderStatus.Active &&
                (!category.HasValue || provider.Category == category.Value) &&
                (!modality.HasValue && !minPriceCrc.HasValue && !maxPriceCrc.HasValue ||
                 db.ProviderServices.Any(service => service.ServiceProviderId == provider.Id &&
                    service.Status == ProviderServiceStatus.Published &&
                    (!modality.HasValue || service.Modality == modality.Value) &&
                    (!minPriceCrc.HasValue || service.PriceCrc >= minPriceCrc.Value) &&
                    (!maxPriceCrc.HasValue || service.PriceCrc <= maxPriceCrc.Value))))
            .OrderByDescending(provider => provider.IsFeatured).ThenBy(provider => provider.Name)
            .Skip(skip).Take(take)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<ServiceProvider>> GetNearbyActivePagedAsync(
        ServiceProviderCategory? category,
        ServiceModality? modality,
        decimal? minPriceCrc,
        decimal? maxPriceCrc,
        decimal centerLat,
        decimal centerLng,
        int radiusKm,
        int skip,
        int take,
        CancellationToken ct = default)
    {
        var (deltaLat, deltaLng) = GeoHelper.BoundingBoxDelta((double)centerLat, radiusKm * 1_000);
        var candidates = await db.ServiceProviders.AsNoTracking()
            .Where(provider => provider.Status == ServiceProviderStatus.Active &&
                provider.Lat >= centerLat - (decimal)deltaLat && provider.Lat <= centerLat + (decimal)deltaLat &&
                provider.Lng >= centerLng - (decimal)deltaLng && provider.Lng <= centerLng + (decimal)deltaLng &&
                (!category.HasValue || provider.Category == category.Value) &&
                (!modality.HasValue && !minPriceCrc.HasValue && !maxPriceCrc.HasValue ||
                 db.ProviderServices.Any(service => service.ServiceProviderId == provider.Id &&
                    service.Status == ProviderServiceStatus.Published &&
                    (!modality.HasValue || service.Modality == modality.Value) &&
                    (!minPriceCrc.HasValue || service.PriceCrc >= minPriceCrc.Value) &&
                    (!maxPriceCrc.HasValue || service.PriceCrc <= maxPriceCrc.Value))))
            .OrderByDescending(provider => provider.IsFeatured).ThenBy(provider => provider.Name)
            .Take(5_000)
            .ToListAsync(ct);

        return candidates
            .Select(provider => new { Provider = provider, Distance = GeoHelper.DistanceMetres((double)centerLat, (double)centerLng, (double)provider.Lat, (double)provider.Lng) })
            .Where(candidate => candidate.Distance <= radiusKm * 1_000d)
            .OrderBy(candidate => candidate.Distance).ThenByDescending(candidate => candidate.Provider.IsFeatured).ThenBy(candidate => candidate.Provider.Name)
            .Skip(skip).Take(take).Select(candidate => candidate.Provider).ToList();
    }

    public async Task<IReadOnlyList<ServiceProvider>> GetOperationalProvidersAsync(int skip, int take, CancellationToken ct = default) =>
        await db.ServiceProviders.AsNoTracking()
            .Where(provider => provider.Status == ServiceProviderStatus.Active || provider.Status == ServiceProviderStatus.Suspended)
            .OrderBy(provider => provider.Status).ThenBy(provider => provider.Name)
            .Skip(skip).Take(take).ToListAsync(ct);

    public async Task AddAsync(ServiceProvider serviceProvider, CancellationToken ct = default) =>
        await db.ServiceProviders.AddAsync(serviceProvider, ct);

    public void Update(ServiceProvider serviceProvider) => db.ServiceProviders.Update(serviceProvider);

    public Task<ProviderService?> GetServiceByIdAsync(Guid serviceId, CancellationToken ct = default) =>
        db.ProviderServices.AsNoTracking().FirstOrDefaultAsync(service => service.Id == serviceId, ct);

    public async Task<IReadOnlyList<ProviderService>> GetServicesByProviderAsync(Guid serviceProviderId, CancellationToken ct = default) =>
        await db.ProviderServices.AsNoTracking()
            .Where(service => service.ServiceProviderId == serviceProviderId)
            .OrderBy(service => service.Name)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<ProviderService>> GetPublishedServicesByProviderAsync(Guid serviceProviderId, CancellationToken ct = default) =>
        await db.ProviderServices.AsNoTracking()
            .Where(service => service.ServiceProviderId == serviceProviderId && service.Status == ProviderServiceStatus.Published)
            .OrderBy(service => service.Name)
            .ToListAsync(ct);

    public async Task AddServiceAsync(ProviderService service, CancellationToken ct = default) =>
        await db.ProviderServices.AddAsync(service, ct);

    public void UpdateService(ProviderService service) => db.ProviderServices.Update(service);

    public async Task AddBookingAsync(ProviderBooking booking, CancellationToken ct = default) =>
        await db.ProviderBookings.AddAsync(booking, ct);

    public async Task<IReadOnlyList<ServiceAvailabilityRule>> GetActiveAvailabilityRulesAsync(
        Guid providerServiceId, CancellationToken ct = default) =>
        await db.ServiceAvailabilityRules.AsNoTracking()
            .Where(rule => rule.ProviderServiceId == providerServiceId && rule.IsActive)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<ServiceAvailabilityRule>> GetAvailabilityRulesAsync(Guid providerServiceId, CancellationToken ct = default) =>
        await db.ServiceAvailabilityRules.AsNoTracking().Where(rule => rule.ProviderServiceId == providerServiceId)
            .OrderBy(rule => rule.DayOfWeek).ThenBy(rule => rule.StartsAtLocalTime).ToListAsync(ct);

    public Task<ServiceAvailabilityRule?> GetAvailabilityRuleByIdAsync(Guid ruleId, CancellationToken ct = default) =>
        db.ServiceAvailabilityRules.FirstOrDefaultAsync(rule => rule.Id == ruleId, ct);

    public async Task AddAvailabilityRuleAsync(ServiceAvailabilityRule rule, CancellationToken ct = default) =>
        await db.ServiceAvailabilityRules.AddAsync(rule, ct);

    public void UpdateAvailabilityRule(ServiceAvailabilityRule rule) => db.ServiceAvailabilityRules.Update(rule);

    public async Task<IReadOnlyList<ServiceAvailabilityBlock>> GetAvailabilityBlocksByServiceRangeAsync(
        Guid providerServiceId, DateTimeOffset rangeStart, DateTimeOffset rangeEnd, CancellationToken ct = default) =>
        await db.ServiceAvailabilityBlocks.AsNoTracking().Where(block => block.ProviderServiceId == providerServiceId &&
            block.IsActive && block.StartsAt < rangeEnd && rangeStart < block.EndsAt).OrderBy(block => block.StartsAt).Take(500).ToListAsync(ct);

    public Task<ServiceAvailabilityBlock?> GetAvailabilityBlockByIdAsync(Guid blockId, CancellationToken ct = default) =>
        db.ServiceAvailabilityBlocks.FirstOrDefaultAsync(block => block.Id == blockId, ct);

    public async Task AddAvailabilityBlockAsync(ServiceAvailabilityBlock block, CancellationToken ct = default) =>
        await db.ServiceAvailabilityBlocks.AddAsync(block, ct);

    public void UpdateAvailabilityBlock(ServiceAvailabilityBlock block) => db.ServiceAvailabilityBlocks.Update(block);

    public async Task<IReadOnlyList<ProviderBooking>> GetBookingsByServiceRangeAsync(
        Guid providerServiceId, DateTimeOffset rangeStart, DateTimeOffset rangeEnd, CancellationToken ct = default) =>
        await db.ProviderBookings.AsNoTracking()
            .Where(booking => booking.ProviderServiceId == providerServiceId &&
                booking.StartsAt < rangeEnd && rangeStart < booking.EndsAt &&
                booking.Status != ProviderBookingStatus.CancelledByCustomer &&
                booking.Status != ProviderBookingStatus.CancelledByProvider &&
                booking.Status != ProviderBookingStatus.NoShow &&
                booking.Status != ProviderBookingStatus.Disputed &&
                booking.Status != ProviderBookingStatus.Refunded)
            .OrderBy(booking => booking.StartsAt)
            .Take(5_000)
            .ToListAsync(ct);

    public Task<ProviderBooking?> GetBookingByIdAsync(Guid bookingId, CancellationToken ct = default) =>
        db.ProviderBookings.FirstOrDefaultAsync(booking => booking.Id == bookingId, ct);

    public async Task<IReadOnlyList<ProviderBooking>> GetBookingsByCustomerAsync(Guid customerUserId, int skip, int take, CancellationToken ct = default) =>
        await db.ProviderBookings.AsNoTracking().Where(booking => booking.CustomerUserId == customerUserId)
            .OrderByDescending(booking => booking.StartsAt).Skip(skip).Take(take).ToListAsync(ct);

    public async Task<IReadOnlyList<ProviderBooking>> GetBookingsByProviderAsync(Guid serviceProviderId, int skip, int take, CancellationToken ct = default) =>
        await db.ProviderBookings.AsNoTracking().Where(booking => booking.ServiceProviderId == serviceProviderId)
            .OrderByDescending(booking => booking.StartsAt).Skip(skip).Take(take).ToListAsync(ct);

    public async Task<IReadOnlyList<ProviderBooking>> GetRequestedBookingsCreatedBeforeAsync(DateTimeOffset cutoff, int take, CancellationToken ct = default) =>
        await db.ProviderBookings.Where(booking => booking.Status == ProviderBookingStatus.Requested && booking.CreatedAt < cutoff)
            .OrderBy(booking => booking.CreatedAt).Take(take).ToListAsync(ct);

    public async Task<IReadOnlyList<ProviderBooking>> GetPaymentPendingBookingsCreatedBeforeAsync(DateTimeOffset cutoff, int take, CancellationToken ct = default) =>
        await db.ProviderBookings.Where(booking => booking.Status == ProviderBookingStatus.AwaitingPayment && booking.CreatedAt < cutoff)
            .OrderBy(booking => booking.CreatedAt).Take(take).ToListAsync(ct);

    public async Task<IReadOnlyList<ProviderBooking>> GetConfirmedBookingsStartingBetweenAsync(
        DateTimeOffset startsAfter, DateTimeOffset startsBefore, int take, CancellationToken ct = default) =>
        await db.ProviderBookings.AsNoTracking()
            .Where(booking => booking.Status == ProviderBookingStatus.Confirmed &&
                booking.StartsAt >= startsAfter && booking.StartsAt < startsBefore)
            .OrderBy(booking => booking.StartsAt).Take(take).ToListAsync(ct);

    public Task<ProviderVerification?> GetLatestVerificationAsync(Guid serviceProviderId, CancellationToken ct = default) =>
        db.ProviderVerifications.OrderByDescending(verification => verification.SubmittedAt)
            .FirstOrDefaultAsync(verification => verification.ServiceProviderId == serviceProviderId, ct);

    public Task<ProviderVerification?> GetVerificationByIdAsync(Guid verificationId, CancellationToken ct = default) =>
        db.ProviderVerifications.FirstOrDefaultAsync(verification => verification.Id == verificationId, ct);

    public Task<ProviderPayment?> GetPaymentByIdAsync(Guid paymentId, CancellationToken ct = default) =>
        db.ProviderPayments.FirstOrDefaultAsync(payment => payment.Id == paymentId, ct);

    public Task<ProviderPayment?> GetPaymentByBookingAsync(Guid bookingId, CancellationToken ct = default) =>
        db.ProviderPayments.AsNoTracking().FirstOrDefaultAsync(payment => payment.BookingId == bookingId, ct);

    public Task<ProviderPayment?> GetPaymentByIdempotencyKeyAsync(string idempotencyKey, CancellationToken ct = default) =>
        db.ProviderPayments.AsNoTracking().FirstOrDefaultAsync(payment => payment.IdempotencyKey == idempotencyKey, ct);

    public async Task AddPaymentAsync(ProviderPayment payment, CancellationToken ct = default) =>
        await db.ProviderPayments.AddAsync(payment, ct);

    public void UpdatePayment(ProviderPayment payment) => db.ProviderPayments.Update(payment);

    public Task<ProviderIncident?> GetIncidentByIdAsync(Guid incidentId, CancellationToken ct = default) =>
        db.ProviderIncidents.FirstOrDefaultAsync(incident => incident.Id == incidentId, ct);

    public async Task<IReadOnlyList<ProviderIncident>> GetIncidentsByProviderAsync(Guid serviceProviderId, int skip, int take, CancellationToken ct = default) =>
        await db.ProviderIncidents.AsNoTracking()
            .Where(incident => incident.ServiceProviderId == serviceProviderId)
            .OrderByDescending(incident => incident.CreatedAt)
            .Skip(skip).Take(take).ToListAsync(ct);

    public async Task<IReadOnlyList<ProviderIncident>> GetOperationalIncidentsAsync(int skip, int take, CancellationToken ct = default) =>
        await db.ProviderIncidents.AsNoTracking()
            .OrderBy(incident => incident.Status).ThenBy(incident => incident.CreatedAt)
            .Skip(skip).Take(take).ToListAsync(ct);

    public async Task AddIncidentAsync(ProviderIncident incident, CancellationToken ct = default) =>
        await db.ProviderIncidents.AddAsync(incident, ct);

    public void UpdateIncident(ProviderIncident incident) => db.ProviderIncidents.Update(incident);

    public async Task<IReadOnlyList<ProviderVerification>> GetPendingVerificationsAsync(int skip, int take, CancellationToken ct = default) =>
        await db.ProviderVerifications.AsNoTracking()
            .Where(verification => verification.Status == ProviderVerificationStatus.Pending)
            .OrderBy(verification => verification.SubmittedAt)
            .Skip(skip).Take(take).ToListAsync(ct);

    public async Task<IReadOnlyList<ProviderVerification>> GetVerifiedVerificationsExpiredBeforeAsync(DateOnly date, int take, CancellationToken ct = default) =>
        await db.ProviderVerifications.Where(verification => verification.Status == ProviderVerificationStatus.Verified && verification.ExpiresAt < date)
            .OrderBy(verification => verification.ExpiresAt).Take(take).ToListAsync(ct);

    public async Task<IReadOnlyList<ProviderVerification>> GetVerificationsExpiringWithinAsync(int days, CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var limit = today.AddDays(days);
        return await db.ProviderVerifications.AsNoTracking()
            .Where(verification => verification.Status == ProviderVerificationStatus.Verified &&
                verification.ExpiresAt >= today && verification.ExpiresAt <= limit)
            .OrderBy(verification => verification.ExpiresAt).Take(500).ToListAsync(ct);
    }

    public async Task<IReadOnlySet<Guid>> GetActiveVerifiedProviderIdsAsync(IEnumerable<Guid> providerIds, CancellationToken ct = default)
    {
        var ids = providerIds.Distinct().ToList();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var verified = await db.ProviderVerifications.AsNoTracking()
            .Where(verification => ids.Contains(verification.ServiceProviderId) &&
                verification.Status == ProviderVerificationStatus.Verified &&
                verification.SupersededAt == null && verification.ExpiresAt >= today)
            .Select(verification => verification.ServiceProviderId)
            .Distinct().ToListAsync(ct);
        return verified.ToHashSet();
    }

    public async Task SupersedeActiveVerificationsAsync(Guid serviceProviderId, Guid exceptVerificationId, CancellationToken ct = default)
    {
        var activeVerifications = await db.ProviderVerifications.Where(verification =>
            verification.ServiceProviderId == serviceProviderId &&
            verification.Id != exceptVerificationId &&
            verification.Status == ProviderVerificationStatus.Verified &&
            verification.SupersededAt == null).ToListAsync(ct);
        foreach (var verification in activeVerifications)
            verification.Supersede();
    }

    public async Task AddVerificationAsync(ProviderVerification verification, CancellationToken ct = default) =>
        await db.ProviderVerifications.AddAsync(verification, ct);

    public void UpdateVerification(ProviderVerification verification) => db.ProviderVerifications.Update(verification);

    public void UpdateBooking(ProviderBooking booking) => db.ProviderBookings.Update(booking);

    public async Task<bool> TryAddBookingAsync(ProviderBooking booking, int capacity, CancellationToken ct = default)
    {
        var strategy = db.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
            var reservedCapacity = await db.ProviderBookings
                .Where(existing => existing.ProviderServiceId == booking.ProviderServiceId &&
                    existing.StartsAt < booking.EndsAt && booking.StartsAt < existing.EndsAt &&
                    existing.Status != ProviderBookingStatus.CancelledByCustomer &&
                    existing.Status != ProviderBookingStatus.CancelledByProvider &&
                    existing.Status != ProviderBookingStatus.NoShow &&
                    existing.Status != ProviderBookingStatus.Expired &&
                    existing.Status != ProviderBookingStatus.Disputed &&
                    existing.Status != ProviderBookingStatus.Refunded)
                .SumAsync(existing => (int?)existing.Quantity, ct) ?? 0;

            if (reservedCapacity + booking.Quantity > capacity)
            {
                await transaction.RollbackAsync(ct);
                return false;
            }

            await db.ProviderBookings.AddAsync(booking, ct);
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return true;
        });
    }

    public async Task<bool> TryRescheduleBookingAsync(
        ProviderBooking booking, DateTimeOffset startsAt, int durationMinutes, int capacity, CancellationToken ct = default)
    {
        var endsAt = startsAt.AddMinutes(durationMinutes);
        var strategy = db.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
            var reservedCapacity = await db.ProviderBookings
                .Where(existing => existing.ProviderServiceId == booking.ProviderServiceId && existing.Id != booking.Id &&
                    existing.StartsAt < endsAt && startsAt < existing.EndsAt &&
                    existing.Status != ProviderBookingStatus.CancelledByCustomer &&
                    existing.Status != ProviderBookingStatus.CancelledByProvider &&
                    existing.Status != ProviderBookingStatus.NoShow &&
                    existing.Status != ProviderBookingStatus.Expired &&
                    existing.Status != ProviderBookingStatus.Disputed &&
                    existing.Status != ProviderBookingStatus.Refunded)
                .SumAsync(existing => (int?)existing.Quantity, ct) ?? 0;
            if (reservedCapacity + booking.Quantity > capacity)
            {
                await transaction.RollbackAsync(ct);
                return false;
            }

            booking.Reschedule(startsAt, durationMinutes);
            db.ProviderBookings.Update(booking);
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return true;
        });
    }
}