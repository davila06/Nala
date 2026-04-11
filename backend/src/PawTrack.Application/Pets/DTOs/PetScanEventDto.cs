using PawTrack.Domain.Pets;

namespace PawTrack.Application.Pets.DTOs;

public sealed record PetScanEventDto(
    DateTimeOffset ScannedAt,
    string? CityName,
    string? CountryCode,
    string DeviceSummary)
{
    public static PetScanEventDto FromDomain(QrScanEvent scanEvent) => new(
        scanEvent.ScannedAt,
        scanEvent.CityName,
        scanEvent.CountryCode,
        ToDeviceSummary(scanEvent.UserAgent));

    private static string ToDeviceSummary(string? userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent))
            return "Dispositivo desconocido";

        var ua = userAgent.ToLowerInvariant();

        if (ua.Contains("iphone")) return "iPhone";
        if (ua.Contains("android")) return "Android";
        if (ua.Contains("windows")) return "Windows";
        if (ua.Contains("macintosh") || ua.Contains("mac os")) return "Mac";
        if (ua.Contains("ipad")) return "iPad";

        return "Navegador web";
    }
}

/// <summary>
/// Extended record that includes an HMAC-SHA256 signature over the event payload,
/// allowing forensic verification that the export has not been tampered with.
/// Signature format: "sha256={hex}" — null when no signing key is configured.
/// </summary>
public sealed record PetScanHistoryDto(
    int ScansToday,
    IReadOnlyList<PetScanEventDto> Events,
    string? Signature,
    DateTimeOffset? SignedAt);
