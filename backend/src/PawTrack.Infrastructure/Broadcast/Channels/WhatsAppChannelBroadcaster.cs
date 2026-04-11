using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Broadcast;

namespace PawTrack.Infrastructure.Broadcast.Channels;

/// <summary>
/// WhatsApp channel broadcaster using the Meta Cloud API (WhatsApp Business).
/// <para>
/// To activate in production:
/// 1. Create a Meta Business account and get WhatsApp Business API access.
/// 2. Configure <c>Broadcast:WhatsApp:PhoneNumberId</c> and
///    <c>Broadcast:WhatsApp:AccessToken</c> in Key Vault.
/// 3. Replace the stub body with the actual HTTP client call to
///    <c>https://graph.facebook.com/v19.0/{PhoneNumberId}/messages</c>.
/// </para>
/// <para>
/// Idempotency: pass <c>LostPetEventId</c> as the message's biz-opaque id
/// so Meta deduplicates re-sends within a 24-hour window.
/// </para>
/// </summary>
public sealed class WhatsAppChannelBroadcaster(
    IConfiguration configuration,
    ILogger<WhatsAppChannelBroadcaster> logger)
    : IChannelBroadcaster
{
    public BroadcastChannel Channel => BroadcastChannel.WhatsApp;

    public bool IsEnabled =>
        !string.IsNullOrWhiteSpace(configuration["Broadcast:WhatsApp:PhoneNumberId"]) &&
        !string.IsNullOrWhiteSpace(configuration["Broadcast:WhatsApp:AccessToken"]);

    public Task<string?> SendAsync(
        BroadcastMessageContext context,
        CancellationToken cancellationToken = default)
    {
        // Production implementation (commented until credentials are available):
        //
        // var client = httpClientFactory.CreateClient("WhatsApp");
        // var payload = new
        // {
        //     messaging_product = "whatsapp",
        //     recipient_type = "individual",
        //     to = ownerWhatsAppNumber,  // stored separately on user profile
        //     type = "template",
        //     template = new
        //     {
        //         name = "lost_pet_broadcast",
        //         language = new { code = "es_CR" },
        //         components = new[]
        //         {
        //             new { type = "body", parameters = new[] {
        //                 new { type = "text", text = context.PetName },
        //                 new { type = "text", text = context.TrackingUrl },
        //             }}
        //         }
        //     },
        //     biz_opaque_callback_data = context.LostPetEventId.ToString()
        // };
        // var response = await client.PostAsJsonAsync("messages", payload, cancellationToken);
        // response.EnsureSuccessStatusCode();
        // var body = await response.Content.ReadFromJsonAsync<WhatsAppSendResponse>();
        // return body?.Messages?.FirstOrDefault()?.Id;

        logger.LogInformation(
            "WhatsApp broadcast skipped (credentials not configured) for event {EventId}",
            context.LostPetEventId);

        // Return a sentinel so the orchestrator can tell that this was
        // a stub call rather than a channel failure.
        return Task.FromResult<string?>(null);
    }
}
