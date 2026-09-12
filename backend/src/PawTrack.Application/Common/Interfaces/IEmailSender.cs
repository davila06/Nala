namespace PawTrack.Application.Common.Interfaces;

public interface IEmailSender
{
    Task SendEmailVerificationAsync(string to, string name, string verificationToken, CancellationToken cancellationToken = default);

    Task SendPasswordResetAsync(string to, string name, string resetToken, CancellationToken cancellationToken = default);

    Task SendLostPetAlertAsync(string to, string ownerName, string petName, CancellationToken cancellationToken = default);

    Task SendPetReunitedAsync(string to, string ownerName, string petName, CancellationToken cancellationToken = default);
    Task SendFamilyInvitationAsync(string to, string token, CancellationToken cancellationToken = default);

    Task SendSightingAlertAsync(string to, string ownerName, string petName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends the multi-channel broadcast email to the owner.
    /// Richer than <see cref="SendLostPetAlertAsync"/> — includes tracking link,
    /// profile URL, photo URL, and last-seen timestamp.
    /// </summary>
    Task SendBroadcastLostPetAsync(
        string to,
        string ownerContactName,
        string petName,
        string petProfileUrl,
        string trackingUrl,
        string? recentPhotoUrl,
        DateTimeOffset lastSeenAt,
        IReadOnlyList<NearbyClinicRef>? nearbyFeaturedClinics = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Notifies a pet owner that a found-pet report may match their lost pet.
    /// </summary>
    Task SendFoundPetMatchAsync(
        string to,
        string ownerName,
        string petName,
        int scorePercent,
        CancellationToken cancellationToken = default);

    Task SendStaleReportReminderAsync(
        string to,
        string ownerName,
        string petName,
        CancellationToken cancellationToken = default);

    Task SendCustodyStartedAsync(
        string to,
        string recipientName,
        string petName,
        string counterpartName,
        int expectedDays,
        CancellationToken cancellationToken = default);

    Task SendCustodyClosedAsync(
        string to,
        string recipientName,
        string petName,
        string counterpartName,
        string outcome,
        CancellationToken cancellationToken = default);

    // ── Bundle order emails ───────────────────────────────────────────────────
    Task SendBundleOrderConfirmationAsync(
        string to, string name, string collarModelLabel,
        string paymentReference, decimal amountCrc, string shippingAddress,
        CancellationToken cancellationToken = default);

    Task SendBundlePaymentConfirmedAsync(
        string to, string name, string collarModelLabel,
        CancellationToken cancellationToken = default);

    Task SendBundleShippedAsync(
        string to, string name, string collarModelLabel, string trackingNumber,
        CancellationToken cancellationToken = default);

    // ── Subscription lifecycle emails ─────────────────────────────────────────
    Task SendSubscriptionExpiringAsync(
        string to, string name, string tierLabel, DateTimeOffset expiresAt,
        CancellationToken cancellationToken = default);

    Task SendSubscriptionExpiredAsync(
        string to, string name, string tierLabel,
        CancellationToken cancellationToken = default);

    // ── Clinic lifecycle emails ───────────────────────────────────────────────
    Task SendClinicApprovedWelcomeAsync(
        string to, string clinicName, string loginUrl,
        CancellationToken cancellationToken = default);

    // ── Partner review emails (Stores & Service Providers) ────────────────────
    Task SendStoreApprovedWelcomeAsync(
        string to, string storeName, string loginUrl,
        CancellationToken cancellationToken = default);

    Task SendStoreReviewedNoticeAsync(
        string to, string storeName, bool approved,
        CancellationToken cancellationToken = default);

    Task SendServiceProviderApprovedWelcomeAsync(
        string to, string providerName, string loginUrl,
        CancellationToken cancellationToken = default);

    Task SendServiceProviderReviewedNoticeAsync(
        string to, string providerName, bool approved,
        CancellationToken cancellationToken = default);

    // ── Adoption lifecycle emails ─────────────────────────────────────────────
    Task SendAdoptionInterestAsync(
        string to, string shelterName, string animalName, string applicantName, string applicationId,
        CancellationToken cancellationToken = default);

    Task SendAdoptionApprovedAsync(
        string to, string applicantName, string animalName,
        CancellationToken cancellationToken = default);

    Task SendAdoptionRejectedAsync(
        string to, string applicantName, string animalName,
        CancellationToken cancellationToken = default);

    // ── Service Provider Booking emails ───────────────────────────────────────
    Task SendProviderBookingCreatedCustomerAsync(
        string to, string customerName, string providerName, string serviceName, DateTimeOffset startsAt,
        CancellationToken cancellationToken = default);

    Task SendProviderBookingCreatedProviderAsync(
        string to, string providerName, string customerName, string serviceName, DateTimeOffset startsAt,
        CancellationToken cancellationToken = default);

    // ── Local Store Order emails ──────────────────────────────────────────────
    Task SendStoreOrderPlacedCustomerAsync(
        string to, string customerName, string storeName, string orderRef, decimal totalCrc,
        CancellationToken cancellationToken = default);

    Task SendStoreOrderPlacedStoreAsync(
        string to, string storeName, string customerName, string orderRef, decimal totalCrc,
        CancellationToken cancellationToken = default);

    Task SendStoreOrderConfirmedCustomerAsync(
        string to, string customerName, string storeName, string orderRef, string? note,
        CancellationToken cancellationToken = default);

    // ── Collar / Safety emails ────────────────────────────────────────────────
    Task SendCollarSafeZoneBreachAsync(
        string to, string ownerName, string petName, string safeZoneName,
        CancellationToken cancellationToken = default);

    // ── Auth & Security emails ────────────────────────────────────────────────
    Task SendPasswordResetSuccessAsync(
        string to, string name,
        CancellationToken cancellationToken = default);

    // ── Recurring billing emails ──────────────────────────────────────────────
    Task SendRecurringPaymentReceiptAsync(
        string to, string name, string tierLabel, decimal amountCrc, string last4, DateTimeOffset nextExpiry,
        CancellationToken cancellationToken = default);

    Task SendRecurringPaymentFailedAsync(
        string to, string name, string tierLabel, string reason, DateTimeOffset gracePeriodExpiry,
        CancellationToken cancellationToken = default);
}

