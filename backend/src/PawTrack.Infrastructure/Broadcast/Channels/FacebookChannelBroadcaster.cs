using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Broadcast;

namespace PawTrack.Infrastructure.Broadcast.Channels;

/// <summary>
/// Facebook channel broadcaster using the Graph API Pages Feed endpoint.
/// Requires <c>Broadcast:Facebook:PageAccessToken</c> and <c>Broadcast:Facebook:PageId</c> in Key Vault.
/// </summary>
public sealed class FacebookChannelBroadcaster(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    ILogger<FacebookChannelBroadcaster> logger)
    : IChannelBroadcaster
{
    private const string GraphApiVersion = "v19.0";

    public BroadcastChannel Channel => BroadcastChannel.Facebook;

    public bool IsEnabled =>
        !string.IsNullOrWhiteSpace(configuration["Broadcast:Facebook:PageAccessToken"]) &&
        !string.IsNullOrWhiteSpace(configuration["Broadcast:Facebook:PageId"]);

    public async Task<string?> SendAsync(
        BroadcastMessageContext context,
        CancellationToken cancellationToken = default)
    {
        var accessToken = configuration["Broadcast:Facebook:PageAccessToken"];
        var pageId = configuration["Broadcast:Facebook:PageId"];

        if (string.IsNullOrWhiteSpace(accessToken) || string.IsNullOrWhiteSpace(pageId))
        {
            logger.LogInformation(
                "Facebook broadcast skipped — credentials not configured for event {EventId}",
                context.LostPetEventId);
            return null;
        }

        var message = BuildMessage(context);
        var url = $"https://graph.facebook.com/{GraphApiVersion}/{pageId}/feed";
        var payload = new { message, link = context.PetProfileUrl, access_token = accessToken };

        var client = httpClientFactory.CreateClient("Facebook");
        var response = await client.PostAsJsonAsync(url, payload, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            logger.LogError(
                "Facebook broadcast failed for event {EventId}. Status={Status} Body={Body}",
                context.LostPetEventId, (int)response.StatusCode, body);
            return null;
        }

        var result = await response.Content.ReadFromJsonAsync<FacebookPostResult>(cancellationToken: cancellationToken);
        logger.LogInformation(
            "Facebook broadcast posted for event {EventId}. PostId={PostId}",
            context.LostPetEventId, result?.Id);
        return result?.Id;
    }

    private static string BuildMessage(BroadcastMessageContext ctx)
    {
        var species = ctx.PetSpecies == "Dog" ? "Perro" : ctx.PetSpecies == "Cat" ? "Gato" : ctx.PetSpecies;
        var breed = ctx.PetBreed is not null ? $" ({ctx.PetBreed})" : string.Empty;
        var description = !string.IsNullOrWhiteSpace(ctx.LastSeenDescription)
            ? "\n" + ctx.LastSeenDescription
            : string.Empty;

        return
            "MASCOTA PERDIDA: " + ctx.PetName + "\n" +
            species + breed + " | Visto: " + ctx.LastSeenAt.ToString("dd/MM HH:mm") +
            description + "\n\n" +
            "Reportar: " + ctx.TrackingUrl + "\n" +
            "Perfil: " + ctx.PetProfileUrl + "\n\n" +
            "#MascotaPerdida #PawTrackCR #CostaRica";
    }

    private sealed record FacebookPostResult(string? Id);
}
