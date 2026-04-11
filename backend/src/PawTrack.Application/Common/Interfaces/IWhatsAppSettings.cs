namespace PawTrack.Application.Common.Interfaces;

/// <summary>
/// Provides WhatsApp bot configuration values to Application layer handlers.
/// Implemented by the Infrastructure layer using <c>IConfiguration</c>.
/// </summary>
public interface IWhatsAppSettings
{
    /// <summary>
    /// The hub verification token registered in the Meta App Dashboard.
    /// Must match the <c>hub.verify_token</c> query parameter Meta sends on webhook registration.
    /// </summary>
    string? VerifyToken { get; }
}
