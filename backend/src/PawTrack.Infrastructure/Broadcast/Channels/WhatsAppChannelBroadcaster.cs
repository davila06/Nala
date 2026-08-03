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

        var phoneNumberId  = configuration["Broadcast:WhatsApp:PhoneNumberId"];
        var accessToken    = configuration["Broadcast:WhatsApp:AccessToken"];
        var recipientListUrl = configuration["Broadcast:WhatsApp:RecipientListUrl"]; // comma-sep WA numbers

        if (string.IsNullOrWhiteSpace(phoneNumberId) || string.IsNullOrWhiteSpace(accessToken))
        {
            logger.LogWarning(
                "WhatsApp broadcast skipped — credentials not configured. EventId={EventId}",
                context.LostPetEventId);
            return null;
        }

        var body = BuildMessageBody(context);
        var recipients = (recipientListUrl ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (recipients.Length == 0)
        {
            logger.LogWarning("WhatsApp broadcast: no recipients configured. EventId={EventId}", context.LostPetEventId);
            return null;
        }

        var client = httpClientFactory.CreateClient("MetaWhatsApp");
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

        string? lastMessageId = null;
        foreach (var to in recipients)
        {
            var payload = new
            {
                messaging_product = "whatsapp",
                recipient_type    = "individual",
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
        }

        return lastMessageId;
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
