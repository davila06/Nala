namespace PawTrack.Application.Common.Settings;

/// <summary>
/// Settings for the WhatsApp Bot module.
/// Bind via "Bot:*" in appsettings.
/// </summary>
public sealed class BotSettings
{
    /// <summary>
    /// HMAC-SHA256 secret used to hash E.164 phone numbers before storage.
    /// Must be at least 32 characters. Override via Key Vault in production.
    /// </summary>
    public string PhoneHashSecret { get; init; } = string.Empty;
}
