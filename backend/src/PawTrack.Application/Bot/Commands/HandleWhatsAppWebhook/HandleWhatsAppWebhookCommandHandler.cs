using System.Security.Cryptography;
using System.Text;
using MediatR;
using Microsoft.Extensions.Logging;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Auth;
using PawTrack.Domain.Bot;
using PawTrack.Domain.Common;
using PawTrack.Domain.LostPets;
using PawTrack.Domain.Pets;

namespace PawTrack.Application.Bot.Commands.HandleWhatsAppWebhook;

/// <summary>
/// State-machine handler for inbound WhatsApp messages.
/// <para>
/// Flow: AwaitingPetName → AwaitingLastSeenTime → AwaitingLocation → Completed.
/// Each step persists progress to <see cref="BotSession"/> so that conversations
/// survive server restarts and the 24-hour Meta session window.
/// </para>
/// </summary>
public sealed class HandleWhatsAppWebhookCommandHandler(
    IBotSessionRepository botSessionRepository,
    IUserRepository userRepository,
    IPetRepository petRepository,
    ILostPetRepository lostPetRepository,
    IWhatsAppSender whatsAppSender,
    IGeocodingService geocodingService,
    IPublicAppUrlProvider urlProvider,
    IUnitOfWork unitOfWork,
    ILogger<HandleWhatsAppWebhookCommandHandler> logger)
    : IRequestHandler<HandleWhatsAppWebhookCommand, Result<Unit>>
{
    public async Task<Result<Unit>> Handle(
        HandleWhatsAppWebhookCommand request, CancellationToken cancellationToken)
    {
        var phoneHash = HashPhone(request.WaId);

        // Load or create session
        var session = await botSessionRepository.GetActiveByPhoneHashAsync(phoneHash, cancellationToken);

        if (session is null)
        {
            session = BotSession.Create(phoneHash);
            await botSessionRepository.AddAsync(session, cancellationToken);
        }
        else if (session.IsExpired())
        {
            session.Expire();
            await botSessionRepository.UpdateAsync(session, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            // Create a fresh session
            session = BotSession.Create(phoneHash);
            await botSessionRepository.AddAsync(session, cancellationToken);
        }

        // Idempotency guard — Meta may re-deliver webhooks
        if (session.IsMessageProcessed(request.MessageId))
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Success(Unit.Value);
        }

        session.MarkMessageProcessed(request.MessageId);

        await DispatchByStepAsync(session, request, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(Unit.Value);
    }

    // ── Step dispatcher ───────────────────────────────────────────────────────

    private async Task DispatchByStepAsync(
        BotSession session,
        HandleWhatsAppWebhookCommand request,
        CancellationToken ct)
    {
        switch (session.Step)
        {
            case BotStep.AwaitingPetName:
                await HandleAwaitingPetNameAsync(session, request, ct);
                break;

            case BotStep.AwaitingLastSeenTime:
                await HandleAwaitingLastSeenTimeAsync(session, request, ct);
                break;

            case BotStep.AwaitingLocation:
                await HandleAwaitingLocationAsync(session, request, ct);
                break;

            case BotStep.Completed:
                await whatsAppSender.SendTextAsync(request.WaId,
                    "🐾 Ya tienes un reporte activo para este número. " +
                    "Si tu mascota ya apareció o quieres hacer un nuevo reporte, " +
                    "escribe *nuevo* para reiniciar.",
                    ct);
                break;

            case BotStep.Expired:
                // Should not happen because we reset before dispatching, but guard anyway
                logger.LogWarning("Unexpected Expired session reached dispatcher for hash {Hash}", session.PhoneNumberHash);
                break;
        }
    }

    // ── Step: AwaitingPetName ─────────────────────────────────────────────────

    private async Task HandleAwaitingPetNameAsync(
        BotSession session, HandleWhatsAppWebhookCommand request, CancellationToken ct)
    {
        // Any text message is treated as the pet name.
        // Non-text messages (e.g. image, sticker) prompt a clarification.
        if (request.MessageType != "text" || string.IsNullOrWhiteSpace(request.TextBody))
        {
            await whatsAppSender.SendTextAsync(request.WaId,
                "🐾 Hola! Soy el bot de *PawTrack CR*. Voy a ayudarte a reportar a tu mascota perdida.\n\n" +
                "Para comenzar, dime: ¿Cuál es el *nombre* de tu mascota?",
                ct);
            return;
        }

        // "nuevo" resets a completed session — but since Completed sessions are not returned
        // by GetActiveByPhoneHash, this handles the first message in any new session.
        session.SetPetName(request.TextBody);
        await botSessionRepository.UpdateAsync(session, ct);

        await whatsAppSender.SendTextAsync(request.WaId,
            $"Perfecto 🐕 ¿Cuándo fue la última vez que viste a *{session.PetName}*? " +
            "(Ejemplo: hoy a las 3 p.m., ayer en la noche)",
            ct);
    }

    // ── Step: AwaitingLastSeenTime ────────────────────────────────────────────

    private async Task HandleAwaitingLastSeenTimeAsync(
        BotSession session, HandleWhatsAppWebhookCommand request, CancellationToken ct)
    {
        if (request.MessageType != "text" || string.IsNullOrWhiteSpace(request.TextBody))
        {
            await whatsAppSender.SendTextAsync(request.WaId,
                $"¿Cuándo fue la última vez que viste a *{session.PetName}*? " +
                "(Escribe una fecha/hora, por ejemplo: hoy a las 3 p.m.)",
                ct);
            return;
        }

        var parsedTime = TryParseSpanishTime(request.TextBody);
        session.SetLastSeenTime(request.TextBody, parsedTime);
        await botSessionRepository.UpdateAsync(session, ct);

        await whatsAppSender.SendTextAsync(request.WaId,
            $"Entendido ⏰ ¿Dónde fue visto/a *{session.PetName}* por última vez? " +
            "Puedes escribir la dirección o compartir tu ubicación 📍",
            ct);
    }

    // ── Step: AwaitingLocation ────────────────────────────────────────────────

    private async Task HandleAwaitingLocationAsync(
        BotSession session, HandleWhatsAppWebhookCommand request, CancellationToken ct)
    {
        double? lat = null;
        double? lng = null;
        string locationRaw;

        if (request.MessageType == "location" && request.LocationLat.HasValue && request.LocationLng.HasValue)
        {
            lat = request.LocationLat;
            lng = request.LocationLng;
            locationRaw = request.LocationAddress ?? $"{lat},{lng}";
        }
        else if (request.MessageType == "text" && !string.IsNullOrWhiteSpace(request.TextBody))
        {
            locationRaw = request.TextBody;
            (lat, lng) = await geocodingService.GeocodeAsync(request.TextBody, ct);
        }
        else
        {
            await whatsAppSender.SendTextAsync(request.WaId,
                $"¿Dónde fue visto/a *{session.PetName}* por última vez? " +
                "Escribe la dirección o comparte tu ubicación 📍",
                ct);
            return;
        }

        session.SetLocation(locationRaw, lat, lng);

        // Create guest user, pet, and lost report
        var (guestUser, pet, lostEvent) = await CreateReportAsync(session, ct);

        session.Complete(guestUser.Id, pet.Id, lostEvent.Id);
        await botSessionRepository.UpdateAsync(session, ct);

        var baseUrl = urlProvider.GetBaseUrl();
        var profileUrl = $"{baseUrl}/pets/{pet.Id}";

        await whatsAppSender.SendTextAsync(request.WaId,
            $"✅ ¡Reporte creado para *{session.PetName}*!\n\n" +
            $"🔗 Comparte este link con tus vecinos:\n{profileUrl}\n\n" +
            "💡 Si alguien lo ve, podrá registrar un avistamiento en el mapa. " +
            "Crea una cuenta en *pawtrack.cr* con este número para gestionar el caso desde la app.",
            ct);

        logger.LogInformation(
            "WhatsApp bot created lost report {LostEventId} for pet {PetId} (guest user {UserId})",
            lostEvent.Id, pet.Id, guestUser.Id);
    }

    // ── Report creation ───────────────────────────────────────────────────────

    private async Task<(User GuestUser, Pet Pet, LostPetEvent LostEvent)> CreateReportAsync(
        BotSession session, CancellationToken ct)
    {
        // Synthetic email keyed on phone hash (first 12 hex chars for brevity)
        var shortHash = session.PhoneNumberHash[..12];
        var syntheticEmail = $"wa-{shortHash}@bot.pawtrack.cr";

        // Reuse existing guest account for this phone number if one was already created
        var guestUser = await userRepository.GetByEmailAsync(syntheticEmail, ct);
        if (guestUser is null)
        {
            guestUser = User.CreateGuestForBot(syntheticEmail, "Usuario WhatsApp");
            await userRepository.AddAsync(guestUser, ct);
        }

        var pet = Pet.Create(
            guestUser.Id,
            session.PetName!,
            PetSpecies.Dog,   // default; user can update after claiming account
            breed: null,
            birthDate: null);

        await petRepository.AddAsync(pet, ct);

        var lostEvent = LostPetEvent.Create(
            pet.Id,
            guestUser.Id,
            description: $"Reportado vía WhatsApp Bot. Última ubicación conocida: {session.LocationRaw}",
            lastSeenLat: session.LastSeenLat,
            lastSeenLng: session.LastSeenLng,
            lastSeenAt: session.LastSeenAt ?? DateTimeOffset.UtcNow,
            publicMessage: $"¡Ayuda! Se perdió {session.PetName}. Última vez visto/a: {session.LastSeenRaw}.",
            contactName: null,
            contactPhone: null);

        await lostPetRepository.AddAsync(lostEvent, ct);

        return (guestUser, pet, lostEvent);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>SHA-256 hash of the E.164 phone number. Never persists the raw number.</summary>
    private static string HashPhone(string waId)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(waId));
        return Convert.ToHexStringLower(bytes);
    }

    /// <summary>
    /// Best-effort Spanish time parser for MVP.
    /// Returns UtcNow on any parse failure so the report is always created.
    /// </summary>
    private static DateTimeOffset TryParseSpanishTime(string input)
    {
        // Normalise to lower-case for keyword matching
        var lower = input.ToLowerInvariant().Trim();

        var now = DateTimeOffset.UtcNow;

        if (lower.StartsWith("hoy", StringComparison.Ordinal))    return now.Date;
        if (lower.StartsWith("ayer", StringComparison.Ordinal))   return now.Date.AddDays(-1);

        // Try a direct parse (handles "2025-04-04 14:30" etc.)
        if (DateTimeOffset.TryParse(input, out var parsed))
            return parsed;

        return now; // fallback
    }
}
