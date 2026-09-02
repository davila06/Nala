using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Broadcast;

namespace PawTrack.Infrastructure.Broadcast.Channels;

/// <summary>
/// Telegram channel broadcaster using the Bot API sendMessage endpoint.
/// Requires <c>Broadcast:Telegram:BotToken</c> and <c>Broadcast:Telegram:ChatId</c> in Key Vault.
/// </summary>
public sealed class TelegramChannelBroadcaster(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    ILogger<TelegramChannelBroadcaster> logger)
    : IChannelBroadcaster
{
    public BroadcastChannel Channel => BroadcastChannel.Telegram;

    public bool IsEnabled =>
        !string.IsNullOrWhiteSpace(configuration["Broadcast:Telegram:BotToken"]) &&
        !string.IsNullOrWhiteSpace(configuration["Broadcast:Telegram:ChatId"]);

    public async Task<string?> SendAsync(
        BroadcastMessageContext context,
        CancellationToken cancellationToken = default)
    {
        var botToken = configuration["Broadcast:Telegram:BotToken"];
        var chatId = configuration["Broadcast:Telegram:ChatId"];

        if (string.IsNullOrWhiteSpace(botToken) || string.IsNullOrWhiteSpace(chatId))
        {
            logger.LogInformation(
                "Telegram broadcast skipped — credentials not configured for event {EventId}",
                context.LostPetEventId);
            return null;
        }

        var text = BuildMessage(context);
        var url = $"https://api.telegram.org/bot{botToken}/sendMessage";
        var payload = new
        {
            chat_id = chatId,
            text,
            parse_mode = "HTML",
            disable_web_page_preview = false,
        };

        var client = httpClientFactory.CreateClient("Telegram");
        var response = await client.PostAsJsonAsync(url, payload, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            logger.LogError(
                "Telegram broadcast failed for event {EventId}. Status={Status} Body={Body}",
                context.LostPetEventId, (int)response.StatusCode, body);
            return null;
        }

        var result = await response.Content.ReadFromJsonAsync<TelegramResult>(cancellationToken: cancellationToken);
        var messageId = result?.Result?.MessageId.ToString();
        logger.LogInformation(
            "Telegram broadcast sent for event {EventId}. MessageId={MessageId}",
            context.LostPetEventId, messageId);

        // Sponsor logo — same rationale as WhatsApp: Telegram text messages can't embed
        // an inline image, so the sponsoring clinic's logo goes out as its own photo message.
        var sponsorClinic = context.NearbyFeaturedClinics?.FirstOrDefault(c => c.LogoUrl is not null);
        if (sponsorClinic is not null)
            await SendSponsorLogoAsync(client, botToken, chatId, sponsorClinic, context.LostPetEventId, cancellationToken);

        return messageId;
    }

    /// <summary>
    /// Delivers the sponsoring clinic's logo as its own Telegram photo message.
    /// Best-effort — failures here never affect the main alert delivery.
    /// </summary>
    private async Task SendSponsorLogoAsync(
        HttpClient client, string botToken, string chatId, NearbyClinicRef clinic,
        Guid lostPetEventId, CancellationToken cancellationToken)
    {
        try
        {
            var photoUrl = $"https://api.telegram.org/bot{botToken}/sendPhoto";
            var photoPayload = new
            {
                chat_id = chatId,
                photo = clinic.LogoUrl,
                caption = $"🏥 Patrocinado por {clinic.Name} — clínica veterinaria cercana",
            };

            var response = await client.PostAsJsonAsync(photoUrl, photoPayload, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                logger.LogWarning(
                    "Telegram sponsor logo send failed. Clinic={Clinic} Status={Status} Body={Body}",
                    clinic.Name, (int)response.StatusCode, body);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "Telegram sponsor logo error. Clinic={Clinic} EventId={EventId}",
                clinic.Name, lostPetEventId);
        }
    }

    private static string BuildMessage(BroadcastMessageContext ctx)
    {
        var species = ctx.PetSpecies == "Dog" ? "Perro" : ctx.PetSpecies == "Cat" ? "Gato" : ctx.PetSpecies;
        var breed = ctx.PetBreed is not null ? " - " + ctx.PetBreed : string.Empty;
        var desc = !string.IsNullOrWhiteSpace(ctx.LastSeenDescription)
            ? "\n" + ctx.LastSeenDescription
            : string.Empty;

        var clinicsSection = string.Empty;
        if (ctx.NearbyFeaturedClinics?.Count > 0)
        {
            var lines = ctx.NearbyFeaturedClinics.Select(c =>
                "• " + c.Name + (c.PhoneNumber is not null ? " — " + c.PhoneNumber : string.Empty));
            clinicsSection = "\n\n🏥 <b>Clínicas veterinarias cercanas:</b>\n" + string.Join("\n", lines);
        }

        return
            "<b>MASCOTA PERDIDA</b>\n" +
            "<b>" + ctx.PetName + "</b> (" + species + breed + ")\n" +
            "Visto: " + ctx.LastSeenAt.ToString("dd/MM HH:mm") + desc + "\n\n" +
            "<a href=\"" + ctx.TrackingUrl + "\">Reportar si lo ves</a>\n" +
            "<a href=\"" + ctx.PetProfileUrl + "\">Ver perfil completo</a>" +
            clinicsSection + "\n\n" +
            "#MascotaPerdida #PawTrackCR";
    }

    private sealed record TelegramResult(
        [property: JsonPropertyName("result")] TelegramMessage? Result);

    private sealed record TelegramMessage(
        [property: JsonPropertyName("message_id")] int MessageId);
}
