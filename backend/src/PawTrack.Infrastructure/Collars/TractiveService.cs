using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PawTrack.Application.Collars.Interfaces;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;

namespace PawTrack.Infrastructure.Collars;

/// <summary>
/// Tractive OAuth2 (Authorization Code flow) + REST location polling.
/// Docs: https://developers.tractive.com/api-reference/
/// </summary>
public sealed class TractiveService(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    ILogger<TractiveService> logger) : ITractiveService
{
    private const string AuthBase = "https://my.tractive.com";
    private const string ApiBase = "https://graph.tractive.com/3";
    private const string RedirectUri = "https://api.pawtrack.cr/api/collars/tractive/callback";

    private string ClientId => configuration["Tractive:ClientId"] ?? string.Empty;
    private string ClientSecret => configuration["Tractive:ClientSecret"] ?? string.Empty;
    private string EncryptKey => configuration["Tractive:EncryptKey"] ?? string.Empty;

    public string GetAuthorizationUrl(string state)
    {
        return $"{AuthBase}/api/1/user/oauth/authorize" +
               $"?response_type=code&client_id={Uri.EscapeDataString(ClientId)}" +
               $"&redirect_uri={Uri.EscapeDataString(RedirectUri)}" +
               $"&scope=activity%20device_info" +
               $"&state={Uri.EscapeDataString(state)}";
    }

    public async Task<string> ExchangeCodeForTokenAsync(string code, CancellationToken cancellationToken = default)
    {
        var client = httpClientFactory.CreateClient("Tractive");

        var response = await client.PostAsJsonAsync(
            $"{AuthBase}/api/1/user/oauth/token",
            new
            {
                grant_type = "authorization_code",
                client_id = ClientId,
                client_secret = ClientSecret,
                redirect_uri = RedirectUri,
                code,
            },
            cancellationToken);

        response.EnsureSuccessStatusCode();
        var token = await response.Content.ReadFromJsonAsync<TractiveTokenResponse>(cancellationToken: cancellationToken);

        return Encrypt(token!.AccessToken);
    }

    public async Task<TractivePosition?> GetLatestPositionAsync(
        string encryptedToken,
        string deviceId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var token = Decrypt(encryptedToken);
            var client = httpClientFactory.CreateClient("Tractive");
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var tracker = await client.GetFromJsonAsync<TractiveTrackerResponse>(
                $"{ApiBase}/device_hw_report/{deviceId}",
                cancellationToken);

            if (tracker?.LatLong is null) return null;

            return new TractivePosition(
                tracker.LatLong[0],
                tracker.LatLong[1],
                tracker.BatteryLevel);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Tractive position fetch failed for device {DeviceId}", deviceId);
            return null;
        }
    }

    // ── AES-256-GCM encryption for stored OAuth tokens ───────────────────────

    private string Encrypt(string plainText)
    {
        var key = DeriveKey();
        var nonce = new byte[AesGcm.NonceByteSizes.MaxSize];
        var tag = new byte[AesGcm.TagByteSizes.MaxSize];
        var plain = Encoding.UTF8.GetBytes(plainText);
        var cipher = new byte[plain.Length];

        RandomNumberGenerator.Fill(nonce);
        using var aes = new AesGcm(key, tag.Length);
        aes.Encrypt(nonce, plain, cipher, tag);

        return Convert.ToBase64String([.. nonce, .. tag, .. cipher]);
    }

    private string Decrypt(string cipherBase64)
    {
        var key = DeriveKey();
        var all = Convert.FromBase64String(cipherBase64);
        var nonce = all[..AesGcm.NonceByteSizes.MaxSize];
        var tag = all[AesGcm.NonceByteSizes.MaxSize..(AesGcm.NonceByteSizes.MaxSize + AesGcm.TagByteSizes.MaxSize)];
        var cipher = all[(AesGcm.NonceByteSizes.MaxSize + AesGcm.TagByteSizes.MaxSize)..];
        var plain = new byte[cipher.Length];

        using var aes = new AesGcm(key, tag.Length);
        aes.Decrypt(nonce, cipher, tag, plain);

        return Encoding.UTF8.GetString(plain);
    }

    private byte[] DeriveKey() =>
        SHA256.HashData(Encoding.UTF8.GetBytes(EncryptKey));

    private sealed class TractiveTokenResponse
    {
        [JsonPropertyName("access_token")] public string AccessToken { get; set; } = string.Empty;
    }

    private sealed class TractiveTrackerResponse
    {
        [JsonPropertyName("latlong")] public double[]? LatLong { get; set; }
        [JsonPropertyName("battery_level")] public int? BatteryLevel { get; set; }
    }
}
