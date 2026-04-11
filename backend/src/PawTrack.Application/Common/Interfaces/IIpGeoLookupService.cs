namespace PawTrack.Application.Common.Interfaces;

/// <summary>
/// Resolves an IPv4/IPv6 address to an approximate geographic location.
/// Implemented via the Azure Maps Geolocation API.
/// Returns null when the IP cannot be resolved or the service is unavailable.
/// </summary>
public interface IIpGeoLookupService
{
    /// <summary>
    /// Returns the ISO 3166-1 alpha-2 country code (e.g. "CR") and an approximate
    /// city name for the given IP address, or null if resolution fails.
    /// </summary>
    Task<IpGeoLocation?> LookupAsync(string ipAddress, CancellationToken cancellationToken = default);
}

/// <param name="CountryCode">ISO 3166-1 alpha-2 country code.</param>
/// <param name="CityName">Best-effort city name; may be null if only country is resolved.</param>
public sealed record IpGeoLocation(string CountryCode, string? CityName);
