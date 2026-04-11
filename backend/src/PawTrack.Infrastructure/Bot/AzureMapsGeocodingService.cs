using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PawTrack.Application.Common.Interfaces;

namespace PawTrack.Infrastructure.Bot;

/// <summary>
/// Geocodes free-text location descriptions using the Azure Maps Search API.
/// <para>
/// Configuration keys (stored in Azure Key Vault):
/// <list type="bullet">
///   <item><c>AzureMaps:SubscriptionKey</c> — Azure Maps subscription key.</item>
/// </list>
/// </para>
/// Falls back to <c>(null, null)</c> when confidence is below threshold or the
/// API call fails — the handler will create the report without coordinates.
/// </summary>
public sealed class AzureMapsGeocodingService(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    ILogger<AzureMapsGeocodingService> logger)
    : IGeocodingService, IReverseGeocodingService
{
    private const string ApiBase = "https://atlas.microsoft.com/search/address/json";
    private const string ReverseApiBase = "https://atlas.microsoft.com/search/address/reverse/json";
    private const string ApiVersion = "1.0";
    private const double MinConfidenceScore = 0.5;

    public async Task<(double? Latitude, double? Longitude)> GeocodeAsync(
        string locationText, CancellationToken ct = default)
    {
        var subscriptionKey = configuration["AzureMaps:SubscriptionKey"];
        if (string.IsNullOrWhiteSpace(subscriptionKey))
        {
            logger.LogWarning("AzureMaps:SubscriptionKey not configured — geocoding skipped.");
            return (null, null);
        }

        try
        {
            var client = httpClientFactory.CreateClient("AzureMaps");
            var encodedQuery = Uri.EscapeDataString(locationText);
            var url = $"{ApiBase}?api-version={ApiVersion}" +
                      $"&query={encodedQuery}&countrySet=CR&limit=1&language=es-419";

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("Ocp-Apim-Subscription-Key", subscriptionKey);
            using var response = await client.SendAsync(request, ct);

            if (!response.IsSuccessStatusCode)
                return (null, null);

            var result = (await response.Content
                .ReadFromJsonAsync<AzureMapsSearchResponse>(ct))
                ?.Results?.FirstOrDefault();

            if (result is null || result.Score < MinConfidenceScore)
                return (null, null);

            return (result.Position.Lat, result.Position.Lon);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Azure Maps geocoding failed for query '{Query}'", locationText);
            return (null, null);
        }
    }

    public async Task<string?> ResolveCantonAsync(double lat, double lng, CancellationToken ct = default)
    {
        var subscriptionKey = configuration["AzureMaps:SubscriptionKey"];
        if (string.IsNullOrWhiteSpace(subscriptionKey))
        {
            logger.LogWarning("AzureMaps:SubscriptionKey not configured — reverse geocoding skipped.");
            return null;
        }

        try
        {
            var client = httpClientFactory.CreateClient("AzureMaps");
            var latStr = lat.ToString(System.Globalization.CultureInfo.InvariantCulture);
            var lngStr = lng.ToString(System.Globalization.CultureInfo.InvariantCulture);
            var url = $"{ReverseApiBase}?api-version={ApiVersion}" +
                      $"&query={latStr},{lngStr}&language=es-419";

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("Ocp-Apim-Subscription-Key", subscriptionKey);
            using var response = await client.SendAsync(request, ct);

            if (!response.IsSuccessStatusCode)
                return null;

            var parsed = await response.Content
                .ReadFromJsonAsync<AzureMapsReverseResponse>(ct);
            var address = parsed?.Addresses?.FirstOrDefault()?.Address;

            return NormalizeCanton(address?.Municipality)
                   ?? NormalizeCanton(address?.MunicipalitySubdivision)
                   ?? NormalizeCanton(address?.LocalName);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Azure Maps reverse geocoding failed for {Lat},{Lng}", lat, lng);
            return null;
        }
    }

    private static string? NormalizeCanton(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var normalized = value.Trim();
        return normalized.Length == 0 ? null : normalized;
    }

    // ── Response DTOs ─────────────────────────────────────────────────────────

    private sealed class AzureMapsSearchResponse
    {
        [JsonPropertyName("results")]
        public List<AzureMapsResult>? Results { get; set; }
    }

    private sealed class AzureMapsResult
    {
        [JsonPropertyName("score")]
        public double Score { get; set; }

        [JsonPropertyName("position")]
        public AzureMapsPosition Position { get; set; } = new();
    }

    private sealed class AzureMapsPosition
    {
        [JsonPropertyName("lat")]
        public double Lat { get; set; }

        [JsonPropertyName("lon")]
        public double Lon { get; set; }
    }

    private sealed class AzureMapsReverseResponse
    {
        [JsonPropertyName("addresses")]
        public List<AzureMapsReverseAddress>? Addresses { get; set; }
    }

    private sealed class AzureMapsReverseAddress
    {
        [JsonPropertyName("address")]
        public AzureMapsReverseAddressDetail? Address { get; set; }
    }

    private sealed class AzureMapsReverseAddressDetail
    {
        [JsonPropertyName("municipality")]
        public string? Municipality { get; set; }

        [JsonPropertyName("municipalitySubdivision")]
        public string? MunicipalitySubdivision { get; set; }

        [JsonPropertyName("localName")]
        public string? LocalName { get; set; }
    }
}
