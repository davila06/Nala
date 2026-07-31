namespace PawTrack.Application.Collars.Interfaces;

/// <summary>OAuth2 + location polling integration for Tractive GPS devices.</summary>
public interface ITractiveService
{
    /// <summary>Returns the OAuth2 authorization URL to redirect the pet owner to.</summary>
    string GetAuthorizationUrl(string state);

    /// <summary>Exchanges the authorization code for an access token, encrypts and returns it.</summary>
    Task<string> ExchangeCodeForTokenAsync(string code, CancellationToken cancellationToken = default);

    /// <summary>Fetches the latest GPS position for the given Tractive device ID using the stored token.</summary>
    Task<TractivePosition?> GetLatestPositionAsync(
        string encryptedToken,
        string deviceId,
        CancellationToken cancellationToken = default);
}

public sealed record TractivePosition(double Lat, double Lng, int? BatteryPercent);
