using Microsoft.Extensions.Logging;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Locations;
using PawTrack.Domain.Notifications;

namespace PawTrack.Infrastructure.Notifications;

/// <summary>
/// Orchestrates in-app notification creation, email, and push delivery.
/// Each dispatch method is intentionally fire-and-complete (not fire-and-forget)
/// so callers can await the full pipeline.
/// </summary>
public sealed class NotificationDispatcher(
    INotificationRepository notificationRepository,
    IEmailSender emailSender,
    IPushNotificationService pushNotificationService,
    IUserLocationRepository userLocationRepository,
    IAllyProfileRepository allyProfileRepository,
    INotificationRateLimitService rateLimitService,
    IGeofencedAlertLogRepository alertLogRepository,
    IUnitOfWork unitOfWork,
    ILogger<NotificationDispatcher> logger)
    : INotificationDispatcher
{
    public async Task DispatchLostPetAlertAsync(
        Guid ownerId,
        string ownerEmail,
        string ownerName,
        string petName,
        string lostPetEventId,
        CancellationToken cancellationToken = default)
    {
        var title = $"Lost pet report created for {petName}";
        var body = $"Your pet {petName} has been marked as lost. We hope you reunite soon!";

        // 1. In-app notification
        var notification = Notification.Create(
            ownerId,
            NotificationType.LostPetAlert,
            title,
            body,
            lostPetEventId);

        await notificationRepository.AddAsync(notification, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // 2. Email (non-blocking failure tolerance)
        await emailSender.SendLostPetAlertAsync(ownerEmail, ownerName, petName, cancellationToken);

        // 3. Push
        await pushNotificationService.SendAsync(ownerId, title, body, cancellationToken: cancellationToken);
    }

    public async Task DispatchPetReunitedAsync(
        Guid ownerId,
        string ownerEmail,
        string ownerName,
        string petName,
        string lostPetEventId,
        CancellationToken cancellationToken = default)
    {
        var title = $"{petName} has been reunited!";
        var body = $"Great news! Your pet {petName} has been marked as reunited. Welcome home!";

        // 1. In-app notification
        var notification = Notification.Create(
            ownerId,
            NotificationType.PetReunited,
            title,
            body,
            lostPetEventId);

        await notificationRepository.AddAsync(notification, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // 2. Email
        await emailSender.SendPetReunitedAsync(ownerEmail, ownerName, petName, cancellationToken);

        // 3. Push
        await pushNotificationService.SendAsync(ownerId, title, body, cancellationToken: cancellationToken);
    }

    public async Task DispatchSightingAlertAsync(
        Guid ownerId,
        string ownerEmail,
        string ownerName,
        string petName,
        string sightingId,
        CancellationToken cancellationToken = default)
    {
        var title = $"New sighting of {petName}!";
        var body = $"Someone just reported seeing {petName}. Check the details and verify!";

        // 1. In-app notification
        var notification = Notification.Create(
            ownerId,
            NotificationType.SightingAlert,
            title,
            body,
            sightingId);

        await notificationRepository.AddAsync(notification, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // 2. Email
        await emailSender.SendSightingAlertAsync(ownerEmail, ownerName, petName, cancellationToken);

        // 3. Push
        await pushNotificationService.SendAsync(ownerId, title, body, cancellationToken: cancellationToken);
    }

    // ── Geofenced neighbour alerts ────────────────────────────────────────────────

    private const string GeofencedAlertType = "geofenced-lost-pet";

    public async Task DispatchGeofencedLostPetAlertsAsync(
        Guid lostPetEventId,
        string petName,
        string petSpecies,
        string? petBreed,
        double lastSeenLat,
        double lastSeenLng,
        int radiusMetres,
        CancellationToken cancellationToken = default)
    {
        var nearbyUsers = await userLocationRepository.GetNearbyAlertSubscribersAsync(
            lastSeenLat, lastSeenLng, radiusMetres, cancellationToken);

        if (nearbyUsers.Count == 0)
        {
            logger.LogDebug(
                "No opted-in users within {Radius} m of lost pet {EventId}",
                radiusMetres, lostPetEventId);
            return;
        }

        var speciesLabel = petBreed is { Length: > 0 } breed
            ? $"{petSpecies} {breed}"
            : petSpecies;

        var title = $"🐾 Alerta: {petName} está perdido cerca de ti";
        var body  = $"{speciesLabel} · Ayuda a encontrarlo, toca para ver su perfil.";
        var lostEventIdStr = lostPetEventId.ToString();

        var notified = 0;
        var skipped  = 0;

        foreach (var userLocation in nearbyUsers)
        {
            // ── Guard 1: hourly rate-limit ────────────────────────────────────
            if (!rateLimitService.IsAllowed(userLocation.UserId, GeofencedAlertType))
            {
                skipped++;
                continue;
            }

            // ── Guard 2: case-level deduplication ─────────────────────────────
            // Never re-alert the same user for the same lost-pet case, even after
            // the hourly window has reset.
            if (await alertLogRepository.HasBeenAlertedAsync(
                    userLocation.UserId, lostPetEventId, cancellationToken))
            {
                skipped++;
                continue;
            }

            // ── Guard 3: quiet hours ──────────────────────────────────────────
            if (userLocation.IsInQuietHours(DateTimeOffset.UtcNow))
            {
                skipped++;
                continue;
            }

            // Record rate-limit BEFORE awaiting the send so concurrent bursts can't slip through.
            rateLimitService.Record(userLocation.UserId, GeofencedAlertType);

            // Persist case-level dedup log (will be committed in the batch SaveChanges below).
            await alertLogRepository.AddAsync(
                GeofencedAlertLog.Create(userLocation.UserId, lostPetEventId),
                cancellationToken);

            // In-app notification
            var notification = Notification.Create(
                userLocation.UserId,
                NotificationType.LostPetAlert,
                title,
                body,
                lostEventIdStr);

            await notificationRepository.AddAsync(notification, cancellationToken);

            // Push (non-fatal — log and continue)
            try
            {
                await pushNotificationService.SendAsync(userLocation.UserId, title, body, cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex,
                    "Push delivery failed for user {UserId} (geofenced alert {EventId})",
                    userLocation.UserId, lostPetEventId);
            }

            notified++;
        }

        if (notified > 0)
            await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Geofenced alert for {EventId}: {Notified} sent, {Skipped} rate-limited",
            lostPetEventId, notified, skipped);
    }

    public async Task DispatchVerifiedAllyAlertsAsync(
        Guid lostPetEventId,
        string petName,
        string petSpecies,
        string? petBreed,
        double lastSeenLat,
        double lastSeenLng,
        CancellationToken cancellationToken = default)
    {
        var coveringAllies = await allyProfileRepository.GetVerifiedCoveringPointAsync(
            lastSeenLat,
            lastSeenLng,
            cancellationToken);

        if (coveringAllies.Count == 0)
        {
            logger.LogDebug(
                "No verified allies cover lost pet {EventId} at {Lat}, {Lng}",
                lostPetEventId,
                lastSeenLat,
                lastSeenLng);
            return;
        }

        var speciesLabel = petBreed is { Length: > 0 } breed
            ? $"{petSpecies} {breed}"
            : petSpecies;

        var title = $"Alerta operativa: {petName} necesita apoyo en tu zona";
        var body = $"{speciesLabel} reportado como perdido dentro de tu cobertura declarada.";
        var relatedId = lostPetEventId.ToString();

        foreach (var ally in coveringAllies)
        {
            var notification = Notification.Create(
                ally.UserId,
                NotificationType.VerifiedAllyAlert,
                title,
                body,
                relatedId);

            await notificationRepository.AddAsync(notification, cancellationToken);

            try
            {
                await pushNotificationService.SendAsync(ally.UserId, title, body, cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex,
                    "Push delivery failed for verified ally {UserId} (alert {EventId})",
                    ally.UserId,
                    lostPetEventId);
            }
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    // ── Chat notification ──────────────────────────────────────────────────────

    public async Task DispatchNewChatMessageAsync(
        Guid   recipientUserId,
        string recipientEmail,
        string petName,
        string threadId,
        CancellationToken cancellationToken = default)
    {
        var title = $"Nuevo mensaje sobre {petName}";
        var body  = "Alguien te envió un mensaje en PawTrack. Ábrelo para responder.";

        var notification = Notification.Create(
            recipientUserId,
            NotificationType.ChatMessage,
            title,
            body,
            relatedEntityId: threadId);

        await notificationRepository.AddAsync(notification, cancellationToken);

        try
        {
            await pushNotificationService.SendAsync(recipientUserId, title, body, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "Push delivery failed for chat notification to user {UserId} (thread {ThreadId})",
                recipientUserId,
                threadId);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task DispatchFoundPetMatchAsync(
        Guid ownerId,
        string ownerEmail,
        string ownerName,
        string petName,
        Guid foundPetReportId,
        int scorePercent,
        CancellationToken cancellationToken = default)
    {
        var title = $"Posible match encontrado para {petName}";
        var body = $"Recibimos un reporte de mascota encontrada con {scorePercent}% de coincidencia.";

        var notification = Notification.Create(
            ownerId,
            NotificationType.FoundPetMatch,
            title,
            body,
            foundPetReportId.ToString());

        await notificationRepository.AddAsync(notification, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await emailSender.SendFoundPetMatchAsync(
            ownerEmail,
            ownerName,
            petName,
            scorePercent,
            cancellationToken);

        await pushNotificationService.SendAsync(ownerId, title, body, cancellationToken: cancellationToken);
    }

    // ── Resolve-check prompt ─────────────────────────────────────────────────

    public async Task DispatchResolveCheckPromptAsync(
        Guid ownerId,
        Guid lostPetEventId,
        string petName,
        string message,
        CancellationToken cancellationToken = default)
    {
        var title = $"¿Encontraste a {petName}?";

        var notification = Notification.Create(
            ownerId,
            NotificationType.ResolveCheck,
            title,
            message,
            lostPetEventId.ToString());

        await notificationRepository.AddAsync(notification, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var pushMeta = new PushNotificationMetadata(
            Url: "/notifications",
            ResolveCheckNotificationId: notification.Id.ToString(),
            Category: "resolve-check",
            ActionIds: ["resolve-yes", "resolve-no"]);

        try
        {
            await pushNotificationService.SendAsync(ownerId, title, message, pushMeta, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "Push delivery failed for resolve-check prompt to owner {OwnerId} (event {EventId})",
                ownerId, lostPetEventId);
        }
    }

    // ── Custody notifications ─────────────────────────────────────────────────

    public async Task DispatchCustodyStartedAsync(
        Guid custodyRecordId,
        Guid fosterUserId,
        string fosterEmail,
        string fosterName,
        Guid ownerUserId,
        string ownerEmail,
        string ownerName,
        string petName,
        int expectedDays,
        CancellationToken cancellationToken = default)
    {
        var fosterTitle = $"Custodia de {petName} iniciada";
        var fosterBody  = $"Gracias, {fosterName}. Tienes a {petName} por {expectedDays} día(s). Cuídalo bien.";

        var ownerTitle = $"{petName} está en custodia temporal";
        var ownerBody  = $"{fosterName} cuidará a {petName} por {expectedDays} día(s) mientras lo buscas.";

        var recordIdStr = custodyRecordId.ToString();

        // Foster notification
        var fosterNotification = Notification.Create(
            fosterUserId,
            NotificationType.CustodyStarted,
            fosterTitle,
            fosterBody,
            recordIdStr);

        await notificationRepository.AddAsync(fosterNotification, cancellationToken);

        // Owner notification
        var ownerNotification = Notification.Create(
            ownerUserId,
            NotificationType.CustodyStarted,
            ownerTitle,
            ownerBody,
            recordIdStr);

        await notificationRepository.AddAsync(ownerNotification, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Push — non-fatal
        await TrySendPushAsync(fosterUserId, fosterTitle, fosterBody, null, cancellationToken);
        await TrySendPushAsync(ownerUserId, ownerTitle, ownerBody, null, cancellationToken);
    }

    public async Task DispatchCustodyClosedAsync(
        Guid custodyRecordId,
        Guid fosterUserId,
        string fosterEmail,
        string fosterName,
        Guid ownerUserId,
        string ownerEmail,
        string ownerName,
        string petName,
        string outcome,
        CancellationToken cancellationToken = default)
    {
        var fosterTitle = $"Custodia de {petName} finalizada";
        var fosterBody  = $"La custodia de {petName} ha concluido. Resultado: {outcome}. ¡Gracias por tu ayuda!";

        var ownerTitle = $"Custodia de {petName} cerrada";
        var ownerBody  = $"La custodia de {petName} en manos de {fosterName} ha finalizado. Resultado: {outcome}.";

        var recordIdStr = custodyRecordId.ToString();

        var fosterNotification = Notification.Create(
            fosterUserId,
            NotificationType.CustodyClosed,
            fosterTitle,
            fosterBody,
            recordIdStr);

        await notificationRepository.AddAsync(fosterNotification, cancellationToken);

        var ownerNotification = Notification.Create(
            ownerUserId,
            NotificationType.CustodyClosed,
            ownerTitle,
            ownerBody,
            recordIdStr);

        await notificationRepository.AddAsync(ownerNotification, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await TrySendPushAsync(fosterUserId, fosterTitle, fosterBody, null, cancellationToken);
        await TrySendPushAsync(ownerUserId, ownerTitle, ownerBody, null, cancellationToken);
    }

    // ── Clinic scan notification ───────────────────────────────────────────────

    public async Task DispatchClinicScanDetectedAsync(
        Guid ownerId,
        string ownerEmail,
        string ownerName,
        string petName,
        string clinicName,
        string clinicAddress,
        CancellationToken cancellationToken = default)
    {
        var title = $"Tu mascota {petName} fue vista en una clínica";
        var body  = $"{clinicName} ({clinicAddress}) escaneó el identificador de {petName}.";

        var notification = Notification.Create(
            ownerId,
            NotificationType.SystemMessage,
            title,
            body,
            relatedEntityId: null);

        await notificationRepository.AddAsync(notification, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        try
        {
            await pushNotificationService.SendAsync(ownerId, title, body,
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "Push delivery failed for clinic scan notification to user {UserId}",
                ownerId);
        }
    }

    // ── Private helpers ────────────────────────────────────────────────────────

    private async Task TrySendPushAsync(
        Guid userId,
        string title,
        string body,
        PushNotificationMetadata? metadata,
        CancellationToken cancellationToken)
    {
        try
        {
            await pushNotificationService.SendAsync(userId, title, body, metadata, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "Push delivery failed for user {UserId}", userId);
        }
    }
}

