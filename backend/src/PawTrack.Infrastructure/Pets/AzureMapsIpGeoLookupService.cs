using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PawTrack.Application.Common.Interfaces;

namespace PawTrack.Infrastructure.Pets;

/// <summary>
/// Resolves IPv4/IPv6 addresses to country + city using the Azure Maps Geolocation API.
/// Configuration keys: "AzureMaps:SubscriptionKey"
/// Endpoint: GET https://atlas.microsoft.com/geolocation/ip/json?api-version=1.0&ip={ip}
/// The subscription key is passed as the Ocp-Apim-Subscription-Key header (never in the URL).
/// </summary>
internal sealed class AzureMapsIpGeoLookupService(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    ILogger<AzureMapsIpGeoLookupService> logger)
    : IIpGeoLookupService
{
    private const string ClientName = "AzureMaps";
    private const string ApiVersion = "1.0";

    public async Task<IpGeoLocation?> LookupAsync(string ipAddress, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
            return null;

        var subscriptionKey = configuration["AzureMaps:SubscriptionKey"];
        if (string.IsNullOrWhiteSpace(subscriptionKey))
        {
            logger.LogDebug("AzureMaps:SubscriptionKey not configured — skipping IP geo-lookup.");
            return null;
        }

        try
        {
            var client = httpClientFactory.CreateClient(ClientName);
            var url = $"https://atlas.microsoft.com/geolocation/ip/json?api-version={ApiVersion}&ip={Uri.EscapeDataString(ipAddress.Trim())}";

            using var requestMsg = new HttpRequestMessage(HttpMethod.Get, url);
            requestMsg.Headers.Add("Ocp-Apim-Subscription-Key", subscriptionKey);
            var response = await client.SendAsync(requestMsg, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "Azure Maps geo-lookup returned {StatusCode} for IP {Ip}",
                    response.StatusCode, ipAddress);
                return null;
            }

            var result = await response.Content.ReadFromJsonAsync<AzureMapsGeoResponse>(cancellationToken: cancellationToken);
            if (result?.CountryRegion?.IsoCode is not { Length: > 0 } isoCode)
                return null;

            return new IpGeoLocation(isoCode.ToUpperInvariant(), null);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "IP geo-lookup failed for IP {Ip} — falling back to null.", ipAddress);
            return null;
        }
    }

    // ── Response model ────────────────────────────────────────────────────────

    private sealed record AzureMapsGeoResponse(CountryRegion? CountryRegion);
    private sealed record CountryRegion(string? IsoCode);
}
