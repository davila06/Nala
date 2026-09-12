using PawTrack.Application.Common.Interfaces;

namespace PawTrack.IntegrationTests.Infrastructure;

/// <summary>No-op email sender that captures the last verification token for use in tests.</summary>
public sealed class CapturingEmailSender : IEmailSender
{
    public string? LastVerificationToken { get; private set; }

    public Task SendEmailVerificationAsync(string to, string name, string verificationToken, CancellationToken ct = default)
    {
        LastVerificationToken = verificationToken;
        return Task.CompletedTask;
    }

    public Task SendPasswordResetAsync(string to, string name, string resetToken, CancellationToken ct = default) =>
        Task.CompletedTask;

    public Task SendLostPetAlertAsync(string to, string ownerName, string petName, CancellationToken ct = default) =>
        Task.CompletedTask;

    public Task SendPetReunitedAsync(string to, string ownerName, string petName, CancellationToken ct = default) =>
        Task.CompletedTask;

    public Task SendSightingAlertAsync(string to, string ownerName, string petName, CancellationToken ct = default) =>
        Task.CompletedTask;

    public Task SendBroadcastLostPetAsync(string to, string ownerContactName, string petName, string petProfileUrl,
        string trackingUrl, string? recentPhotoUrl, DateTimeOffset lastSeenAt,
        IReadOnlyList<NearbyClinicRef>? nearbyFeaturedClinics = null, CancellationToken ct = default) =>
        Task.CompletedTask;

    public Task SendFoundPetMatchAsync(string to, string ownerName, string petName, int scorePercent, CancellationToken ct = default) =>
        Task.CompletedTask;

    public Task SendStaleReportReminderAsync(string to, string ownerName, string petName, CancellationToken ct = default) =>
        Task.CompletedTask;

    public Task SendCustodyStartedAsync(string to, string recipientName, string petName, string counterpartName,
        int expectedDays, CancellationToken ct = default) => Task.CompletedTask;

    public Task SendCustodyClosedAsync(string to, string recipientName, string petName, string counterpartName,
        string outcome, CancellationToken ct = default) => Task.CompletedTask;

    public Task SendFamilyInvitationAsync(string to, string token, CancellationToken ct = default) =>
        Task.CompletedTask;

    public Task SendBundleOrderConfirmationAsync(string to, string recipientName, string productName,
        string orderId, decimal price, string currency, CancellationToken ct = default) => Task.CompletedTask;

    public Task SendBundlePaymentConfirmedAsync(string to, string recipientName, string orderId, CancellationToken ct = default) =>
        Task.CompletedTask;

    public Task SendBundleShippedAsync(string to, string recipientName, string orderId,
        string trackingCode, CancellationToken ct = default) => Task.CompletedTask;

    public Task SendSubscriptionExpiringAsync(string to, string name, string tierLabel,
        DateTimeOffset expiresAt, CancellationToken ct = default) => Task.CompletedTask;

    public Task SendSubscriptionExpiredAsync(string to, string name, string tierLabel,
        CancellationToken ct = default) => Task.CompletedTask;

    public Task SendClinicApprovedWelcomeAsync(string to, string clinicName, string loginUrl,
        CancellationToken ct = default) => Task.CompletedTask;

    public Task SendStoreApprovedWelcomeAsync(string to, string storeName, string loginUrl,
        CancellationToken ct = default) => Task.CompletedTask;

    public Task SendStoreReviewedNoticeAsync(string to, string storeName, bool approved,
        CancellationToken ct = default) => Task.CompletedTask;

    public Task SendServiceProviderApprovedWelcomeAsync(string to, string providerName, string loginUrl,
        CancellationToken ct = default) => Task.CompletedTask;

    public Task SendServiceProviderReviewedNoticeAsync(string to, string providerName, bool approved,
        CancellationToken ct = default) => Task.CompletedTask;

    public Task SendAdoptionInterestAsync(string to, string shelterName, string animalName, string applicantName, string applicationId,
        CancellationToken ct = default) => Task.CompletedTask;

    public Task SendAdoptionApprovedAsync(string to, string applicantName, string animalName,
        CancellationToken ct = default) => Task.CompletedTask;

    public Task SendAdoptionRejectedAsync(string to, string applicantName, string animalName,
        CancellationToken ct = default) => Task.CompletedTask;

    public Task SendProviderBookingCreatedCustomerAsync(string to, string customerName, string providerName, string serviceName, DateTimeOffset startsAt,
        CancellationToken ct = default) => Task.CompletedTask;

    public Task SendProviderBookingCreatedProviderAsync(string to, string providerName, string customerName, string serviceName, DateTimeOffset startsAt,
        CancellationToken ct = default) => Task.CompletedTask;

    public Task SendStoreOrderPlacedCustomerAsync(string to, string customerName, string storeName, string orderRef, decimal totalCrc,
        CancellationToken ct = default) => Task.CompletedTask;

    public Task SendStoreOrderPlacedStoreAsync(string to, string storeName, string customerName, string orderRef, decimal totalCrc,
        CancellationToken ct = default) => Task.CompletedTask;

    public Task SendStoreOrderConfirmedCustomerAsync(string to, string customerName, string storeName, string orderRef, string? note,
        CancellationToken ct = default) => Task.CompletedTask;

    public Task SendCollarSafeZoneBreachAsync(string to, string ownerName, string petName, string safeZoneName,
        CancellationToken ct = default) => Task.CompletedTask;

    public Task SendPasswordResetSuccessAsync(string to, string name,
        CancellationToken ct = default) => Task.CompletedTask;

    public Task SendRecurringPaymentReceiptAsync(
        string to, string name, string tierLabel, decimal amountCrc, string last4, DateTimeOffset nextExpiry,
        CancellationToken ct = default) => Task.CompletedTask;

    public Task SendRecurringPaymentFailedAsync(
        string to, string name, string tierLabel, string reason, DateTimeOffset gracePeriodExpiry,
        CancellationToken ct = default) => Task.CompletedTask;
}
