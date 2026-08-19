using Microsoft.AspNetCore.SignalR;
using PawTrack.Application.Common.Interfaces;

namespace PawTrack.API.Hubs;

public sealed class SignalRChatNotifier(IHubContext<ChatHub> hub) : IChatNotifier
{
    public async Task NotifyNewMessageAsync(
        string threadId, Guid messageId, Guid senderUserId,
        string body, DateTimeOffset sentAt, CancellationToken ct = default)
    {
        await hub.Clients
            .Group(ChatHub.ThreadGroup(threadId))
            .SendAsync("NewMessage", new
            {
                messageId = messageId.ToString(),
                threadId,
                senderUserId = senderUserId.ToString(),
                body,
                sentAt,
            }, ct);
    }
}
