using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Common.Settings;

namespace PawTrack.Infrastructure.Pets;

/// <summary>
/// Generates and validates stateless HMAC-SHA256 signed tokens for the WhatsApp avatar endpoint.
///
/// Token format (URL-safe base64): {expiry_unix_seconds}|{petId}|{hmac}
/// All three sections are base64url-encoded to produce a single opaque token string.
///
/// Security notes:
/// - Tokens are time-bound; validation rejects expired tokens.
/// - The signing key must be stored in Azure Key Vault in production.
/// - Comparison is performed with CryptographicOperations.FixedTimeEquals to prevent timing attacks.
/// </summary>
public sealed class HmacAvatarTokenService(IOptions<AvatarTokenSettings> options) : IAvatarTokenService
{
    private static readonly Encoding Utf8 = Encoding.UTF8;

    public string Generate(Guid petId)
    {
        var cfg = options.Value;
        var expiry = DateTimeOffset.UtcNow.AddMinutes(cfg.ExpiryMinutes).ToUnixTimeSeconds();
        var payload = BuildPayload(expiry, petId);
        var hmac = ComputeHmac(payload, cfg.SigningKey);

        // Combine payload + hmac into a single URL-safe token
        var raw = $"{payload}|{hmac}";
        return Convert.ToBase64String(Utf8.GetBytes(raw))
            .Replace('+', '-').Replace('/', '_').TrimEnd('=');
    }

    public bool Validate(Guid petId, string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return false;

        try
        {
            // Restore base64 padding
            var padded = token.Replace('-', '+').Replace('_', '/');
            var padding = padded.Length % 4 == 0 ? 0 : 4 - padded.Length % 4;
            padded += new string('=', padding);

            var raw = Utf8.GetString(Convert.FromBase64String(padded));
            var parts = raw.Split('|');
            if (parts.Length != 3)
                return false;

            if (!long.TryParse(parts[0], out var expiry))
                return false;

            // Reject expired tokens
            if (DateTimeOffset.UtcNow.ToUnixTimeSeconds() >= expiry)
                return false;

            if (!Guid.TryParse(parts[1], out var tokenPetId) || tokenPetId != petId)
                return false;

            var cfg = options.Value;
            var payload = BuildPayload(expiry, petId);
            var expectedHmac = ComputeHmac(payload, cfg.SigningKey);

            // Constant-time compare to prevent timing attacks
            var actualBytes   = Utf8.GetBytes(parts[2]);
            var expectedBytes = Utf8.GetBytes(expectedHmac);
            return CryptographicOperations.FixedTimeEquals(actualBytes, expectedBytes);
        }
        catch
        {
            return false;
        }
    }

    private static string BuildPayload(long expiryUnixSeconds, Guid petId)
        => $"{expiryUnixSeconds}|{petId}";

    private static string ComputeHmac(string payload, string key)
    {
        var keyBytes  = Utf8.GetBytes(key);
        var dataBytes = Utf8.GetBytes(payload);
        var hashBytes = HMACSHA256.HashData(keyBytes, dataBytes);
        return Convert.ToHexString(hashBytes);
    }
}
