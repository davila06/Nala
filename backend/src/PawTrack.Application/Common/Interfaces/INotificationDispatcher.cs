namespace PawTrack.Application.Common.Interfaces;

public interface INotificationDispatcher
{
    Task DispatchLostPetAlertAsync(
        Guid ownerId,
        string ownerEmail,
        string ownerName,
        string petName,
        string lostPetEventId,
        CancellationToken cancellationToken = default);

    Task DispatchPetReunitedAsync(
        Guid ownerId,
        string ownerEmail,
        string ownerName,
        string petName,
        string lostPetEventId,
        CancellationToken cancellationToken = default);

    Task DispatchSightingAlertAsync(
        Guid ownerId,
        string ownerEmail,
        string ownerName,
        string petName,
        string sightingId,
        CancellationToken cancellationToken = default);

    Task DispatchResolveCheckPromptAsync(
        Guid ownerId,
        Guid lostPetEventId,
        string petName,
        string message,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a push notification to every opted-in user whose last known location falls
    /// within <paramref name="radiusMetres"/> metres of the lost pet's last seen coordinates.
    /// Applies per-user rate limiting (maximum one geofenced alert per
    /// <see cref="PawTrack.Domain.Locations.GeofenceConstants.RateLimitWindowMinutes"/> minutes).
    /// </summary>
    Task DispatchGeofencedLostPetAlertsAsync(
        Guid lostPetEventId,
        string petName,
        string petSpecies,
        string? petBreed,
        double lastSeenLat,
        double lastSeenLng,
        int radiusMetres,
        CancellationToken cancellationToken = default);

    Task DispatchVerifiedAllyAlertsAsync(
        Guid lostPetEventId,
        string petName,
        string petSpecies,
        string? petBreed,
        double lastSeenLat,
        double lastSeenLng,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Notifies the recipient that a new masked chat message has arrived.
    /// Does NOT include the message body to avoid PII leaks into notification channels.
    /// </summary>
    Task DispatchNewChatMessageAsync(
        Guid   recipientUserId,
        string recipientEmail,
        string petName,
        string threadId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Notifies a pet owner that someone reported finding a pet whose description
    /// closely matches their active lost-pet report.
    /// </summary>
    Task DispatchFoundPetMatchAsync(
        Guid ownerId,
        string ownerEmail,
        string ownerName,
        string petName,
        Guid foundPetReportId,
        int scorePercent,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Notifies a pet owner that their pet was scanned at an affiliated clinic.
    /// </summary>
    Task DispatchClinicScanDetectedAsync(
        Guid ownerId,
        string ownerEmail,
        string ownerName,
        string petName,
        string clinicName,
        string clinicAddress,
        CancellationToken cancellationToken = default);

    Task DispatchCustodyStartedAsync(
        Guid custodyRecordId,
        Guid fosterUserId,
        string fosterEmail,
        string fosterName,
        Guid ownerUserId,
        string ownerEmail,
        string ownerName,
        string petName,
        int expectedDays,
        CancellationToken cancellationToken = default);

    Task DispatchCustodyClosedAsync(
        Guid custodyRecordId,
        Guid fosterUserId,
        string fosterEmail,
        string fosterName,
        Guid ownerUserId,
        string ownerEmail,
        string ownerName,
        string petName,
        string outcome,
        CancellationToken cancellationToken = default);
}
