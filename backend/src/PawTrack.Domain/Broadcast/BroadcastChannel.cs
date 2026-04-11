namespace PawTrack.Domain.Broadcast;

/// <summary>
/// Represents all supported distribution channels for a lost-pet broadcast.
/// New channels can be added without changing any existing handler logic
/// as long as a corresponding <c>IChannelBroadcaster</c> implementation is
/// registered in the DI container.
/// </summary>
public enum BroadcastChannel
{
    Email,
    WhatsApp,
    Telegram,
    Facebook,
}
