using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using PawTrack.Application.Common.Interfaces;

namespace PawTrack.Infrastructure.Broadcast;

/// <summary>
/// Generates deterministic, short tracking URLs without any external dependency.
/// Format: {baseUrl}/t/{code}?ch={channel}
///
/// The code is a 8-character base62 string derived from a SHA-256 hash of
/// (lostPetEventId + channel), making it:
/// - Deterministic: same inputs → same code (safe to call in retry scenarios).
/// - Non-guessable: not sequential, requires knowledge of the event ID.
/// - Short: 8 chars gives 62^8 ≈ 218 trillion combinations — effectively collision-free at scale.
///
/// Click tracking is incremented by the <c>TrackingController</c> redirect endpoint.
/// </summary>
public sealed class TrackingLinkService(IConfiguration configuration) : ITrackingLinkService
{
    private const string Base62Chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
    private const int CodeLength = 8;

    public string Generate(Guid lostPetEventId, string channel)
    {
        var baseUrl = configuration["App:BaseUrl"]?.TrimEnd('/') ?? "https://pawtrack.cr";
        var code = GenerateCode(lostPetEventId, channel);
        return $"{baseUrl}/t/{code}?ch={Uri.EscapeDataString(channel)}";
    }

    private static string GenerateCode(Guid lostPetEventId, string channel)
    {
        var input = $"{lostPetEventId}:{channel.ToLowerInvariant()}";
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));

        // Convert first 8 bytes of the hash to base62.
        // Using only 8 bytes (64 bits) is sufficient for the range we need.
        var sb = new StringBuilder(CodeLength);
        for (var i = 0; i < CodeLength; i++)
        {
            sb.Append(Base62Chars[hashBytes[i] % Base62Chars.Length]);
        }

        return sb.ToString();
    }
}
