namespace PawTrack.Application.Common.Interfaces;

/// <summary>Pushes real-time chat events to connected SignalR clients.</summary>
public interface IChatNotifier
{
    /// <summary>Broadcasts a new-message event to all clients subscribed to the thread.</summary>
    Task NotifyNewMessageAsync(string threadId, Guid messageId, Guid senderUserId, string body, DateTimeOffset sentAt, CancellationToken ct = default);
}
