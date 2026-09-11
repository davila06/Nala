using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PawTrack.Application.Collars.Interfaces;

namespace PawTrack.Infrastructure.Collars;

/// <summary>
/// Service implementing communication with Jimi IoT TrackSolid Pro Open API.
/// Supports both single device position retrieval and batch queries (up to 100 IMEIs per call).
/// Config keys: TrackSolid:ApiUrl, TrackSolid:AppKey, TrackSolid:AppSecret, TrackSolid:AccessToken.
/// </summary>
public sealed class TrackSolidService(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    ILogger<TrackSolidService> logger) : ITrackSolidService
{
    private string ApiUrl => configuration["TrackSolid:ApiUrl"] ?? "https://open.tracksolidpro.com/route/rest";
    private string? AppKey => configuration["TrackSolid:AppKey"];
    private string? AppSecret => configuration["TrackSolid:AppSecret"];
    private string? AccessToken => configuration["TrackSolid:AccessToken"];

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(AppKey) &&
        !string.IsNullOrWhiteSpace(AppSecret);

    public async Task<TrackSolidPosition?> GetLatestPositionAsync(
        string imei,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(imei);

        var batch = await GetBatchPositionsAsync([imei], cancellationToken);
        return batch.TryGetValue(imei, out var pos) ? pos : null;
    }

    public async Task<IReadOnlyDictionary<string, TrackSolidPosition>> GetBatchPositionsAsync(
        IEnumerable<string> imeis,
        CancellationToken cancellationToken = default)
    {
        var imeiList = imeis.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList();
        if (imeiList.Count == 0)
            return new Dictionary<string, TrackSolidPosition>();

        if (!IsConfigured)
        {
            logger.LogInformation(
                "TrackSolid credentials not configured. Skipping position query for {Count} device(s).",
                imeiList.Count);
            return new Dictionary<string, TrackSolidPosition>();
        }

        var results = new Dictionary<string, TrackSolidPosition>(StringComparer.OrdinalIgnoreCase);

        // TrackSolid Open API supports batching up to 100 devices per request
        foreach (var chunk in imeiList.Chunk(100))
        {
            try
            {
                var chunkResults = await QueryChunkAsync(chunk, cancellationToken);
                foreach (var kvp in chunkResults)
                {
                    results[kvp.Key] = kvp.Value;
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Failed to query TrackSolid Pro positions for batch of {Count} devices.", chunk.Length);
            }
        }

        return results;
    }

    private async Task<Dictionary<string, TrackSolidPosition>> QueryChunkAsync(
        string[] imeis,
        CancellationToken cancellationToken)
    {
        var client = httpClientFactory.CreateClient("TrackSolid");
        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
        var imeiJoined = string.Join(",", imeis);

        // Parameters map for TrackSolid Open API request
        var parameters = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["method"] = "jimi.device.location.get",
            ["app_key"] = AppKey!,
            ["v"] = "1.0",
            ["format"] = "json",
            ["timestamp"] = timestamp,
            ["imeis"] = imeiJoined,
        };

        if (!string.IsNullOrWhiteSpace(AccessToken))
        {
            parameters["access_token"] = AccessToken;
        }

        // Calculate MD5 signature: app_secret + key1value1key2value2... + app_secret
        parameters["sign"] = GenerateSign(parameters, AppSecret!);

        using var content = new FormUrlEncodedContent(parameters);
        var response = await client.PostAsync(ApiUrl, content, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            logger.LogWarning(
                "TrackSolid Open API request failed. Status: {StatusCode}, Response: {Body}",
                (int)response.StatusCode, errorBody);
            return [];
        }

        var json = await response.Content.ReadFromJsonAsync<TrackSolidApiResponse>(cancellationToken: cancellationToken);
        if (json is null || json.Code != 0 || json.Result is null)
        {
            logger.LogWarning(
                "TrackSolid Open API returned error code {Code}: {Message}",
                json?.Code, json?.Message);
            return [];
        }

        var dict = new Dictionary<string, TrackSolidPosition>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in json.Result)
        {
            if (string.IsNullOrWhiteSpace(item.Imei)) continue;

            var recordedAt = DateTimeOffset.TryParse(item.GpsTime, out var dt)
                ? dt
                : DateTimeOffset.UtcNow;

            dict[item.Imei] = new TrackSolidPosition(
                Lat: item.Lat,
                Lng: item.Lng,
                BatteryPercent: item.Battery,
                RecordedAt: recordedAt,
                AccuracyMeters: item.Accuracy,
                IsOnline: item.Online ?? true);
        }

        return dict;
    }

    private static string GenerateSign(SortedDictionary<string, string> parameters, string secret)
    {
        var sb = new StringBuilder(secret);
        foreach (var (k, v) in parameters)
        {
            if (k == "sign") continue;
            sb.Append(k).Append(v);
        }
        sb.Append(secret);

        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        var hash = MD5.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private sealed record TrackSolidApiResponse
    {
        [JsonPropertyName("code")]
        public int Code { get; init; }

        [JsonPropertyName("message")]
        public string? Message { get; init; }

        [JsonPropertyName("result")]
        public List<TrackSolidDeviceResult>? Result { get; init; }
    }

    private sealed record TrackSolidDeviceResult
    {
        [JsonPropertyName("imei")]
        public string? Imei { get; init; }

        [JsonPropertyName("lat")]
        public double Lat { get; init; }

        [JsonPropertyName("lng")]
        public double Lng { get; init; }

        [JsonPropertyName("gpsTime")]
        public string? GpsTime { get; init; }

        [JsonPropertyName("battery")]
        public int? Battery { get; init; }

        [JsonPropertyName("accuracy")]
        public int? Accuracy { get; init; }

        [JsonPropertyName("online")]
        public bool? Online { get; init; }
    }
}
