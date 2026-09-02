using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Broadcast;

namespace PawTrack.Infrastructure.Broadcast.Channels;

/// <summary>
/// WhatsApp channel broadcaster using the Meta Cloud API (WhatsApp Business).
/// Sends a free-text message (not a template) with the pet details and tracking URL.
/// Configuration keys (Key Vault): Broadcast:WhatsApp:PhoneNumberId, Broadcast:WhatsApp:AccessToken.
/// </summary>
public sealed class WhatsAppChannelBroadcaster(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    ILogger<WhatsAppChannelBroadcaster> logger)
    : IChannelBroadcaster
{
    private const string GraphApiBase = "https://graph.facebook.com/v19.0";

    public BroadcastChannel Channel => BroadcastChannel.WhatsApp;

    public bool IsEnabled =>
        !string.IsNullOrWhiteSpace(configuration["Broadcast:WhatsApp:PhoneNumberId"])
        && !string.IsNullOrWhiteSpace(configuration["Broadcast:WhatsApp:AccessToken"])
        && !string.IsNullOrWhiteSpace(configuration["Broadcast:WhatsApp:RecipientListUrl"]);

    public async Task<string?> SendAsync(
        BroadcastMessageContext context,
        CancellationToken cancellationToken = default)
    {
        if (context.RestrictToPaidChannels) return null; // Free users get email only

        var phoneNumberId = configuration["Broadcast:WhatsApp:PhoneNumberId"];
        var accessToken = configuration["Broadcast:WhatsApp:AccessToken"];
        var recipientListUrl = configuration["Broadcast:WhatsApp:RecipientListUrl"]; // comma-sep WA numbers

        if (string.IsNullOrWhiteSpace(phoneNumberId) || string.IsNullOrWhiteSpace(accessToken))
        {
            logger.LogWarning(
                "WhatsApp broadcast skipped — credentials not configured. EventId={EventId}",
                context.LostPetEventId);
            return null;
        }

        var body = BuildMessageBody(context);

        // Merge owner's WA number (if provided) with the static ally broadcast list
        var staticRecipients = (recipientListUrl ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var recipients = new List<string>(staticRecipients);
        if (!string.IsNullOrWhiteSpace(context.OwnerContactPhone))
        {
            // Normalise CR phone to E.164: strip spaces/dashes, add +506 if needed
            var phone = context.OwnerContactPhone.Trim().Replace(" ", "").Replace("-", "");
            if (!phone.StartsWith("+") && !phone.StartsWith("506"))
                phone = "506" + phone;
            if (!phone.StartsWith("+"))
                phone = "+" + phone;
            if (!recipients.Contains(phone))
                recipients.Insert(0, phone); // owner notification first
        }

        if (recipients.Count == 0)
        {
            logger.LogWarning("WhatsApp broadcast: no recipients configured. EventId={EventId}", context.LostPetEventId);
            return null;
        }

        var client = httpClientFactory.CreateClient("MetaWhatsApp");
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

        // Only the closest featured clinic's logo is sent, and only as a single extra
        // image message — WhatsApp text messages cannot embed an inline image, so a
        // real "logo in the alert" requires a separate media message per Meta's API.
        var sponsorClinic = context.NearbyFeaturedClinics?.FirstOrDefault(c => c.LogoUrl is not null);

        string? lastMessageId = null;
        foreach (var to in recipients)
        {
            var payload = new
            {
                messaging_product = "whatsapp",
                recipient_type = "individual",
                to,
                type = "text",
                text = new { preview_url = true, body },
                biz_opaque_callback_data = context.LostPetEventId.ToString(),
            };
            try
            {
                var response = await client.PostAsJsonAsync(
                    $"{GraphApiBase}/{phoneNumberId}/messages", payload, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<MetaMessageResponse>(cancellationToken: cancellationToken);
                    lastMessageId = result?.Messages?.FirstOrDefault()?.Id;
                }
                else
                {
                    var err = await response.Content.ReadAsStringAsync(cancellationToken);
                    logger.LogWarning("WhatsApp send failed for {To}. Status={Status} Body={Body}", to, response.StatusCode, err);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "WhatsApp broadcast error for {To}. EventId={EventId}", to, context.LostPetEventId);
            }

            if (sponsorClinic is not null)
                await SendSponsorLogoAsync(client, phoneNumberId, to, sponsorClinic, context.LostPetEventId, cancellationToken);
        }

        return lastMessageId;
    }

    /// <summary>
    /// Delivers the sponsoring clinic's logo as its own WhatsApp image message.
    /// Best-effort — failures here never affect the main alert delivery.
    /// </summary>
    private async Task SendSponsorLogoAsync(
        HttpClient client, string phoneNumberId, string to, NearbyClinicRef clinic,
        Guid lostPetEventId, CancellationToken cancellationToken)
    {
        try
        {
            var imagePayload = new
            {
                messaging_product = "whatsapp",
                recipient_type = "individual",
                to,
                type = "image",
                image = new
                {
                    link = clinic.LogoUrl,
                    caption = $"🏥 Patrocinado por {clinic.Name} — clínica veterinaria cercana",
                },
            };

            var response = await client.PostAsJsonAsync(
                $"{GraphApiBase}/{phoneNumberId}/messages", imagePayload, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadAsStringAsync(cancellationToken);
                logger.LogWarning(
                    "WhatsApp sponsor logo send failed for {To}. Clinic={Clinic} Status={Status} Body={Body}",
                    to, clinic.Name, response.StatusCode, err);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "WhatsApp sponsor logo error for {To}. Clinic={Clinic} EventId={EventId}",
                to, clinic.Name, lostPetEventId);
        }
    }

    private static string BuildMessageBody(BroadcastMessageContext ctx)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"🚨 *MASCOTA PERDIDA — {ctx.PetName}*");
        sb.AppendLine();
        sb.AppendLine($"Especie: {ctx.PetSpecies}{(ctx.PetBreed is not null ? $" · {ctx.PetBreed}" : "")}");
        sb.AppendLine($"Vista por última vez: {ctx.LastSeenAt:dd/MM/yyyy HH:mm}");
        if (!string.IsNullOrWhiteSpace(ctx.LastSeenDescription))
            sb.AppendLine($"Lugar: {ctx.LastSeenDescription}");
        sb.AppendLine();
        sb.AppendLine($"🔗 Ver perfil: {ctx.PetProfileUrl}");
        sb.AppendLine($"📍 Seguimiento en vivo: {ctx.TrackingUrl}");

        if (ctx.NearbyFeaturedClinics?.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("🏥 Clínicas veterinarias cercanas:");
            foreach (var c in ctx.NearbyFeaturedClinics)
            {
                sb.Append($"  • {c.Name}");
                if (!string.IsNullOrWhiteSpace(c.PhoneNumber)) sb.Append($" — {c.PhoneNumber}");
                sb.AppendLine();
            }
        }

        sb.AppendLine();
        sb.AppendLine("PawTrack CR — Cada mascota merece volver a casa.");
        return sb.ToString().TrimEnd();
    }

    // Minimal deserialization for Meta API response
    private sealed record MetaMessageResponse(MetaMessageId[]? Messages);
    private sealed record MetaMessageId(string Id);
}
