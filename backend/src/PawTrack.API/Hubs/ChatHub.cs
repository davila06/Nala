using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace PawTrack.API.Hubs;

/// <summary>
/// Real-time hub for chat messages between pet owners and finders.
/// Clients subscribe to a group per thread and receive new messages instantly,
/// eliminating the 10-second polling loop.
/// Hub route: /hubs/chat
/// </summary>
[Authorize]
public sealed class ChatHub : Hub
{
    public async Task JoinThread(string threadId)
    {
        if (!TryGetUserId(out _)) return;
        if (!Guid.TryParse(threadId, out _)) return;
        await Groups.AddToGroupAsync(Context.ConnectionId, ThreadGroup(threadId));
    }

    public async Task LeaveThread(string threadId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, ThreadGroup(threadId));
    }

    /// <summary>Called server-side from SendChatMessageCommandHandler after persisting.</summary>
    public static string ThreadGroup(string threadId) => $"chat-thread-{threadId}";

    private bool TryGetUserId(out Guid userId)
    {
        var raw = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(raw, out userId);
    }
}
