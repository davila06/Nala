using PawTrack.Domain.ServiceProviders;

namespace PawTrack.Application.Common.Interfaces;

public interface IServiceProviderRepository
{
    Task<ServiceProvider?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<ServiceProvider?> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<IReadOnlyList<ServiceProvider>> GetActivePagedAsync(
        ServiceProviderCategory? category,
        ServiceModality? modality,
        decimal? minPriceCrc,
        decimal? maxPriceCrc,
        int skip,
        int take,
        CancellationToken ct = default);
    Task<IReadOnlyList<ServiceProvider>> GetOperationalProvidersAsync(int skip, int take, CancellationToken ct = default);
    Task AddAsync(ServiceProvider serviceProvider, CancellationToken ct = default);
    void Update(ServiceProvider serviceProvider);

    Task<ProviderService?> GetServiceByIdAsync(Guid serviceId, CancellationToken ct = default);
    Task<IReadOnlyList<ProviderService>> GetServicesByProviderAsync(Guid serviceProviderId, CancellationToken ct = default);
    Task<IReadOnlyList<ProviderService>> GetPublishedServicesByProviderAsync(Guid serviceProviderId, CancellationToken ct = default);
    Task AddServiceAsync(ProviderService service, CancellationToken ct = default);
    void UpdateService(ProviderService service);

    Task AddBookingAsync(ProviderBooking booking, CancellationToken ct = default);
    Task<IReadOnlyList<ServiceAvailabilityRule>> GetActiveAvailabilityRulesAsync(
        Guid providerServiceId, CancellationToken ct = default);
    Task<IReadOnlyList<ServiceAvailabilityRule>> GetAvailabilityRulesAsync(Guid providerServiceId, CancellationToken ct = default);
    Task<ServiceAvailabilityRule?> GetAvailabilityRuleByIdAsync(Guid ruleId, CancellationToken ct = default);
    Task AddAvailabilityRuleAsync(ServiceAvailabilityRule rule, CancellationToken ct = default);
    void UpdateAvailabilityRule(ServiceAvailabilityRule rule);
    Task<IReadOnlyList<ServiceAvailabilityBlock>> GetAvailabilityBlocksByServiceRangeAsync(Guid providerServiceId, DateTimeOffset rangeStart, DateTimeOffset rangeEnd, CancellationToken ct = default);
    Task<ServiceAvailabilityBlock?> GetAvailabilityBlockByIdAsync(Guid blockId, CancellationToken ct = default);
    Task AddAvailabilityBlockAsync(ServiceAvailabilityBlock block, CancellationToken ct = default);
    void UpdateAvailabilityBlock(ServiceAvailabilityBlock block);
    Task<IReadOnlyList<ProviderBooking>> GetBookingsByServiceRangeAsync(
        Guid providerServiceId, DateTimeOffset rangeStart, DateTimeOffset rangeEnd, CancellationToken ct = default);
    Task<ProviderBooking?> GetBookingByIdAsync(Guid bookingId, CancellationToken ct = default);
    Task<IReadOnlyList<ProviderBooking>> GetBookingsByCustomerAsync(Guid customerUserId, int skip, int take, CancellationToken ct = default);
    Task<IReadOnlyList<ProviderBooking>> GetBookingsByProviderAsync(Guid serviceProviderId, int skip, int take, CancellationToken ct = default);
    Task<IReadOnlyList<ProviderBooking>> GetRequestedBookingsCreatedBeforeAsync(DateTimeOffset cutoff, int take, CancellationToken ct = default);
    Task<IReadOnlyList<ProviderBooking>> GetConfirmedBookingsStartingBetweenAsync(
        DateTimeOffset startsAfter, DateTimeOffset startsBefore, int take, CancellationToken ct = default);
    Task<ProviderVerification?> GetLatestVerificationAsync(Guid serviceProviderId, CancellationToken ct = default);
    Task<ProviderVerification?> GetVerificationByIdAsync(Guid verificationId, CancellationToken ct = default);
    Task<IReadOnlyList<ProviderVerification>> GetPendingVerificationsAsync(int skip, int take, CancellationToken ct = default);
    Task<IReadOnlyList<ProviderVerification>> GetVerifiedVerificationsExpiredBeforeAsync(DateOnly date, int take, CancellationToken ct = default);
    Task<IReadOnlyList<ProviderVerification>> GetVerificationsExpiringWithinAsync(int days, CancellationToken ct = default);
    Task<IReadOnlySet<Guid>> GetActiveVerifiedProviderIdsAsync(IEnumerable<Guid> providerIds, CancellationToken ct = default);
    Task SupersedeActiveVerificationsAsync(Guid serviceProviderId, Guid exceptVerificationId, CancellationToken ct = default);
    Task AddVerificationAsync(ProviderVerification verification, CancellationToken ct = default);
    void UpdateVerification(ProviderVerification verification);
    void UpdateBooking(ProviderBooking booking);
    Task<bool> TryAddBookingAsync(ProviderBooking booking, int capacity, CancellationToken ct = default);
    Task<bool> TryRescheduleBookingAsync(ProviderBooking booking, DateTimeOffset startsAt, int durationMinutes, int capacity, CancellationToken ct = default);
}