namespace PawTrack.Application.Common.Settings;

/// <summary>
/// Configuration for HMAC-signed ephemeral avatar tokens.
/// Override via appsettings.json under "AvatarToken:*".
/// The signing key must live in Azure Key Vault in production.
/// </summary>
public sealed class AvatarTokenSettings
{
    /// <summary>HMAC-SHA256 signing key (minimum 32 characters). Must be stored in Key Vault.</summary>
    public string SigningKey { get; init; } = string.Empty;

    /// <summary>Token validity window in minutes. Default: 60.</summary>
    public int ExpiryMinutes { get; init; } = 60;
}
