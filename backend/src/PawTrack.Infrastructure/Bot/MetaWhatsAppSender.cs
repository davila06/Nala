using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PawTrack.Application.Common.Interfaces;

namespace PawTrack.Infrastructure.Bot;

/// <summary>
/// Sends outbound WhatsApp messages via the Meta Cloud API (Graph API v19.0).
/// <para>
/// Configuration keys (stored in Azure Key Vault):
/// <list type="bullet">
///   <item><c>WhatsApp:PhoneNumberId</c> — the sender phone number ID from Meta Business dashboard.</item>
///   <item><c>WhatsApp:AccessToken</c> — system user permanent access token.</item>
/// </list>
/// </para>
/// </summary>
public sealed class MetaWhatsAppSender(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    ILogger<MetaWhatsAppSender> logger)
    : IWhatsAppSender
{
    private const string GraphApiBase = "https://graph.facebook.com/v19.0";

    public async Task SendTextAsync(string toWaId, string text, CancellationToken ct = default)
    {
        var phoneNumberId = configuration["WhatsApp:PhoneNumberId"];
        var accessToken   = configuration["WhatsApp:AccessToken"];

        if (string.IsNullOrWhiteSpace(phoneNumberId) || string.IsNullOrWhiteSpace(accessToken))
        {
            logger.LogWarning(
                "WhatsApp credentials not configured — message to {WaId} was NOT sent (bot is in dry-run mode).",
                MaskWaId(toWaId));
            return;
        }

        var client = httpClientFactory.CreateClient("MetaWhatsApp");
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

        var payload = new
        {
            messaging_product = "whatsapp",
            recipient_type    = "individual",
            to                = toWaId,
            type              = "text",
            text              = new { preview_url = false, body = text },
        };

        var url = $"{GraphApiBase}/{phoneNumberId}/messages";

        try
        {
            var response = await client.PostAsJsonAsync(url, payload, ct);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(ct);
                logger.LogError(
                    "Meta Cloud API returned {StatusCode} for message to {WaId}: {Body}",
                    (int)response.StatusCode, MaskWaId(toWaId), body);
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            logger.LogError(ex, "Failed to send WhatsApp message to {WaId}", MaskWaId(toWaId));
        }
    }

    /// <summary>Masks all but the last 4 digits for safe logging.</summary>
    private static string MaskWaId(string waId) =>
        waId.Length > 4 ? $"****{waId[^4..]}" : "****";
}
