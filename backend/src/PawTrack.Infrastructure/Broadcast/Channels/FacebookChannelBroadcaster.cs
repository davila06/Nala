using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Broadcast;

namespace PawTrack.Infrastructure.Broadcast.Channels;

/// <summary>
/// Facebook channel broadcaster using the Graph API.
/// <para>
/// To activate in production:
/// 1. Create a Facebook App and get Page Access Token.
/// 2. Configure <c>Broadcast:Facebook:PageAccessToken</c> and
///    <c>Broadcast:Facebook:PageId</c> in Key Vault.
/// 3. Replace the stub body with a POST to the Page Feed endpoint.
///    Note: Facebook requires the page to be owned by a verified business.
/// </para>
/// </summary>
public sealed class FacebookChannelBroadcaster(
    IConfiguration configuration,
    ILogger<FacebookChannelBroadcaster> logger)
    : IChannelBroadcaster
{
    public BroadcastChannel Channel => BroadcastChannel.Facebook;

    public bool IsEnabled =>
        !string.IsNullOrWhiteSpace(configuration["Broadcast:Facebook:PageAccessToken"]) &&
        !string.IsNullOrWhiteSpace(configuration["Broadcast:Facebook:PageId"]);

    public Task<string?> SendAsync(
        BroadcastMessageContext context,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "Facebook broadcast skipped (credentials not configured) for event {EventId}",
            context.LostPetEventId);

        return Task.FromResult<string?>(null);
    }
}
