namespace PawTrack.Application.Common.Interfaces;

/// <summary>
/// Sends outbound WhatsApp messages via the Meta Cloud API.
/// Implementations must honour the 24-hour session window: within 24 h of the
/// last user message any text message can be sent; after that only approved
/// template messages can be sent.
/// </summary>
public interface IWhatsAppSender
{
    /// <summary>
    /// Sends a plain text message to a WhatsApp phone number.
    /// </summary>
    /// <param name="toWaId">Recipient's WhatsApp ID (E.164 without '+').</param>
    /// <param name="text">Message body (max 4 096 characters).</param>
    Task SendTextAsync(string toWaId, string text, CancellationToken ct = default);
}
