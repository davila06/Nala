using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Broadcast;

namespace PawTrack.Infrastructure.Broadcast.Channels;

/// <summary>
/// Telegram channel broadcaster using the Bot API.
/// <para>
/// To activate in production:
/// 1. Create a bot via @BotFather and obtain the Bot Token.
/// 2. Configure <c>Broadcast:Telegram:BotToken</c> in Key Vault.
/// 3. The recipient is identified by their Telegram chat_id, which must be
///    stored on the user's profile (opt-in flow, see roadmap item #9).
/// 4. Replace the stub body with:
///    POST https://api.telegram.org/bot{BotToken}/sendMessage
///    Body: { chat_id, text (HTML or Markdown), parse_mode }
/// </para>
/// </summary>
public sealed class TelegramChannelBroadcaster(
    IConfiguration configuration,
    ILogger<TelegramChannelBroadcaster> logger)
    : IChannelBroadcaster
{
    public BroadcastChannel Channel => BroadcastChannel.Telegram;

    public bool IsEnabled =>
        !string.IsNullOrWhiteSpace(configuration["Broadcast:Telegram:BotToken"]);

    public Task<string?> SendAsync(
        BroadcastMessageContext context,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "Telegram broadcast skipped (credentials not configured) for event {EventId}",
            context.LostPetEventId);

        return Task.FromResult<string?>(null);
    }
}
