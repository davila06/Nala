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
}

