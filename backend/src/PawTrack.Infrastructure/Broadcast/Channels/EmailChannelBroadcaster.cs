using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Broadcast;

namespace PawTrack.Infrastructure.Broadcast.Channels;

/// <summary>
/// Email channel broadcaster.
/// Sends a rich broadcast email to the pet owner summarising the alert
/// and including the tracking link and profile URL.
/// </summary>
public sealed class EmailChannelBroadcaster(
    IEmailSender emailSender,
    IConfiguration configuration,
    ILogger<EmailChannelBroadcaster> logger)
    : IChannelBroadcaster
{
    public BroadcastChannel Channel => BroadcastChannel.Email;

    public bool IsEnabled =>
        !string.IsNullOrWhiteSpace(configuration["App:BaseUrl"]);

    public async Task<string?> SendAsync(
        BroadcastMessageContext context,
        CancellationToken cancellationToken = default)
    {
        await emailSender.SendBroadcastLostPetAsync(
            to: context.OwnerEmail,
            ownerContactName: context.OwnerContactName ?? "El dueño",
            petName: context.PetName,
            petProfileUrl: context.PetProfileUrl,
            trackingUrl: context.TrackingUrl,
            recentPhotoUrl: context.RecentPhotoUrl,
            lastSeenAt: context.LastSeenAt,
            cancellationToken: cancellationToken);

        logger.LogInformation(
            "Email broadcast sent for lost pet event {EventId} to {Email}",
            context.LostPetEventId, context.OwnerEmail);

        return $"email:{context.LostPetEventId}";
    }
}
