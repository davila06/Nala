using Microsoft.Extensions.Logging;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Subscriptions.Services;
using PawTrack.Domain.Locations;
using PawTrack.Domain.Notifications;

namespace PawTrack.Infrastructure.Notifications;

public sealed class NotificationDispatcher(
    INotificationRepository notificationRepository,
    IEmailSender emailSender,
    IPushNotificationService pushNotificationService,
    IUserLocationRepository userLocationRepository,
    IAllyProfileRepository allyProfileRepository,
    INotificationRateLimitService rateLimitService,
    IGeofencedAlertLogRepository alertLogRepository,
    IFamilyRepository familyRepository,
    ISubscriptionService subscriptionService,
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

        // 3. Push — owner + family members (Familia plan)
        await pushNotificationService.SendAsync(ownerId, title, body, cancellationToken: cancellationToken);

        var isFamilia = await subscriptionService.IsFamiliaAsync(ownerId, cancellationToken);
        if (isFamilia)
        {
            var memberIds = await familyRepository.GetActiveMemberIdsAsync(ownerId, cancellationToken);
            foreach (var memberId in memberIds.Where(id => id != ownerId))
                await TrySendPushAsync(memberId, title, body, null, cancellationToken);
        }
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

        // 3. Push — owner + family members (Familia plan)
        await pushNotificationService.SendAsync(ownerId, title, body, cancellationToken: cancellationToken);

        var isFamilia = await subscriptionService.IsFamiliaAsync(ownerId, cancellationToken);
        if (isFamilia)
        {
            var memberIds = await familyRepository.GetActiveMemberIdsAsync(ownerId, cancellationToken);
            foreach (var memberId in memberIds.Where(id => id != ownerId))
                await pushNotificationService.SendAsync(memberId, title, body, cancellationToken: cancellationToken);
        }
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

        // 3. Push — owner + family members (Familia plan)
        await pushNotificationService.SendAsync(ownerId, title, body, cancellationToken: cancellationToken);

        var isFamilia = await subscriptionService.IsFamiliaAsync(ownerId, cancellationToken);
        if (isFamilia)
        {
            var memberIds = await familyRepository.GetActiveMemberIdsAsync(ownerId, cancellationToken);
            foreach (var memberId in memberIds.Where(id => id != ownerId))
                await TrySendPushAsync(memberId, title, body, null, cancellationToken);
        }
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
        var body = $"{speciesLabel} · Ayuda a encontrarlo, toca para ver su perfil.";
        var lostEventIdStr = lostPetEventId.ToString();

        var notified = 0;
        var skipped = 0;

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
        Guid recipientUserId,
        string recipientEmail,
        string petName,
        string threadId,
        CancellationToken cancellationToken = default)
    {
        var title = $"Nuevo mensaje sobre {petName}";
        var body = "Alguien te envió un mensaje en PawTrack. Ábrelo para responder.";

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
        var fosterBody = $"Gracias, {fosterName}. Tienes a {petName} por {expectedDays} día(s). Cuídalo bien.";

        var ownerTitle = $"{petName} está en custodia temporal";
        var ownerBody = $"{fosterName} cuidará a {petName} por {expectedDays} día(s) mientras lo buscas.";

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
        var fosterBody = $"La custodia de {petName} ha concluido. Resultado: {outcome}. ¡Gracias por tu ayuda!";

        var ownerTitle = $"Custodia de {petName} cerrada";
        var ownerBody = $"La custodia de {petName} en manos de {fosterName} ha finalizado. Resultado: {outcome}.";

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
        var body = $"{clinicName} ({clinicAddress}) escaneó el identificador de {petName}.";

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

    // ── Clinic: lost-pet alert ─────────────────────────────────────────────────

    public async Task DispatchLostPetAlertToClinicAsync(
        Guid clinicUserId,
        Guid lostPetEventId,
        string petName,
        double lostLat,
        double lostLng,
        CancellationToken cancellationToken = default)
    {
        await TrySendPushAsync(
            clinicUserId,
            "🚨 Mascota perdida cerca de tu clínica",
            $"{petName} fue reportada perdida en tu zona.",
            new PushNotificationMetadata(Url: $"/map"),
            cancellationToken);
    }

    public async Task DispatchVetReminderAsync(
        Guid ownerId,
        string petName,
        string reminderTitle,
        DateOnly dueDate,
        CancellationToken cancellationToken = default)
    {
        await TrySendPushAsync(
            ownerId,
            $"📅 Recordatorio veterinario — {petName}",
            $"{reminderTitle} · {dueDate:dd/MM/yyyy}",
            new PushNotificationMetadata(Url: "/dashboard"),
            cancellationToken);
    }

    public async Task DispatchClinicMedicalRecordAddedAsync(
        Guid ownerId,
        string petName,
        string clinicName,
        string recordType,
        CancellationToken cancellationToken = default)
    {
        await TrySendPushAsync(
            ownerId,
            $"🏥 Nuevo registro médico — {petName}",
            $"{clinicName} agregó un registro de {recordType.ToLowerInvariant()} al expediente de {petName}.",
            new PushNotificationMetadata(Url: "/dashboard"),
            cancellationToken);
    }

    public async Task DispatchNeighborLostPetAlertAsync(
        Guid neighborUserId,
        string petName,
        string petSpecies,
        string lostPetEventId,
        double lostLat,
        double lostLng,
        CancellationToken cancellationToken = default)
    {
        await TrySendPushAsync(
            neighborUserId,
            $"🚨 Mascota perdida en tu cuadra — {petName}",
            $"Una {petSpecies.ToLowerInvariant()} llamada {petName} fue reportada perdida cerca de ti. ¡Ayuda a encontrarla!",
            new PushNotificationMetadata(Url: $"/lost/{lostPetEventId}"),
            cancellationToken);
    }

    public async Task DispatchNewStoreOrderAsync(
        Guid storeOwnerUserId,
        string storeName,
        string orderId,
        decimal totalCrc,
        CancellationToken cancellationToken = default)
    {
        // In-app notification (persisted — survives missed push)
        var notification = Notification.Create(
            storeOwnerUserId,
            NotificationType.SystemMessage,
            $"🛍️ Nuevo pedido en {storeName}",
            $"Recibirás ₡{totalCrc:N0}. Confirma el pedido cuando verifiques el pago SINPE.",
            relatedEntityId: orderId);

        await notificationRepository.AddAsync(notification, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await TrySendPushAsync(
            storeOwnerUserId,
            $"🛒 Nuevo pedido en {storeName}",
            $"Recibirás ₡{totalCrc:N0}. Confirma el pedido cuando verifiques el pago SINPE.",
            new PushNotificationMetadata(Url: $"/tienda/portal/ordenes/{orderId}"),
            cancellationToken);
    }

    public async Task DispatchAdoptionInterestAsync(
        Guid shelterUserId,
        string animalName,
        Guid applicationId,
        CancellationToken cancellationToken = default)
    {
        var notification = Notification.Create(
            shelterUserId,
            NotificationType.AdoptionInterest,
            $"Nueva solicitud de adopción para {animalName}",
            "Alguien está interesado en adoptar este animal. Revisa la solicitud en tu panel.",
            relatedEntityId: applicationId.ToString());

        await notificationRepository.AddAsync(notification, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await TrySendPushAsync(
            shelterUserId,
            $"🐾 Solicitud para {animalName}",
            "Alguien quiere adoptarlo. Revisa la solicitud.",
            new PushNotificationMetadata(Url: $"/shelter/dashboard"),
            cancellationToken);
    }

    public async Task DispatchAdoptionApprovedAsync(
        Guid applicantUserId,
        string animalName,
        Guid applicationId,
        CancellationToken cancellationToken = default)
    {
        var notification = Notification.Create(
            applicantUserId,
            NotificationType.AdoptionApproved,
            $"¡Tu solicitud para adoptar a {animalName} fue aprobada!",
            "La organización aprobó tu solicitud. Revisa tu bandeja para coordinar los próximos pasos.",
            relatedEntityId: applicationId.ToString());

        await notificationRepository.AddAsync(notification, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await TrySendPushAsync(
            applicantUserId,
            $"✅ Solicitud aprobada — {animalName}",
            "¡La organización aprobó tu solicitud de adopción!",
            new PushNotificationMetadata(Url: "/mis-adopciones"),
            cancellationToken);
    }

    public async Task DispatchAdoptionRejectedAsync(
        Guid applicantUserId,
        string animalName,
        Guid applicationId,
        CancellationToken cancellationToken = default)
    {
        var notification = Notification.Create(
            applicantUserId,
            NotificationType.AdoptionRejected,
            $"Tu solicitud para adoptar a {animalName} no fue aprobada",
            "La organización no pudo aprobar tu solicitud en esta ocasión. Hay más animales esperando un hogar.",
            relatedEntityId: applicationId.ToString());

        await notificationRepository.AddAsync(notification, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await TrySendPushAsync(
            applicantUserId,
            $"Tu solicitud para {animalName}",
            "La organización no pudo aprobarla. Hay más animales esperando.",
            new PushNotificationMetadata(Url: "/adopciones"),
            cancellationToken);
    }

    public async Task DispatchAdoptionFairAlertAsync(
        Guid fairId,
        string fairTitle,
        double fairLat,
        double fairLng,
        int radiusMetres,
        DateTimeOffset fairStartsAt,
        CancellationToken cancellationToken = default)
    {
        var nearbyUsers = await userLocationRepository.GetNearbyAlertSubscribersAsync(
            fairLat, fairLng, radiusMetres, cancellationToken);

        var dateStr = fairStartsAt.ToLocalTime().ToString("dd MMM, HH:mm");
        var notified = 0;

        foreach (var userLocation in nearbyUsers)
        {
            if (!rateLimitService.IsAllowed(userLocation.UserId, "adoption_fair_alert")) continue;

            var notification = Notification.Create(
                userLocation.UserId,
                NotificationType.AdoptionFairAlert,
                $"🐾 Feria de adopción cerca tuyo: {fairTitle}",
                $"El {dateStr}. ¡Ven a conocer a los animales que buscan hogar!",
                relatedEntityId: fairId.ToString());

            await notificationRepository.AddAsync(notification, cancellationToken);

            await TrySendPushAsync(
                userLocation.UserId,
                $"🐾 Feria de adopción — {fairTitle}",
                $"El {dateStr}, cerca tuyo.",
                new PushNotificationMetadata(Url: "/adopciones/ferias"),
                cancellationToken);

            rateLimitService.Record(userLocation.UserId, "adoption_fair_alert");
            notified++;
        }

        if (notified > 0)
            await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("AdoptionFairAlert dispatched to {Count} users for fair {FairId}", notified, fairId);
    }
}

