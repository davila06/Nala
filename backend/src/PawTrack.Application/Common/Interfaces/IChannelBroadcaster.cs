using PawTrack.Domain.Broadcast;

namespace PawTrack.Application.Common.Interfaces;

/// <summary>
/// Contract for a single-channel broadcast implementation.
/// Each channel (Email, WhatsApp, Telegram, Facebook) provides one concrete class.
/// The orchestrator calls all registered implementations in parallel.
/// </summary>
public interface IChannelBroadcaster
{
    /// <summary>The channel this broadcaster handles.</summary>
    BroadcastChannel Channel { get; }

    /// <summary>
    /// Dispatches the message for the given channel.
    /// Returns an implementation-specific external message ID on success,
    /// or <c>null</c> if the provider does not return an identifier.
    /// Throws on unrecoverable failures so the orchestrator can record the error.
    /// Implementations MUST be idempotent when called with the same
    /// <paramref name="context"/> — use <see cref="BroadcastMessageContext.LostPetEventId"/>
    /// as an idempotency key at the provider level where supported.
    /// </summary>
    Task<string?> SendAsync(BroadcastMessageContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns true when the channel is enabled and has valid credentials.
    /// Called before sending to avoid wasting network calls on misconfigured channels.
    /// </summary>
    bool IsEnabled { get; }
}

/// <summary>
/// Immutable payload passed to every <see cref="IChannelBroadcaster"/> instance.
/// Contains everything needed to compose any channel-specific message.
/// </summary>
public sealed record BroadcastMessageContext(
    Guid LostPetEventId,
    string PetName,
    string PetSpecies,
    string? PetBreed,
    /// <see cref="string"/> Owner email — used by the Email channel broadcaster only.
    string OwnerEmail,
    string? OwnerContactName,
    string PetProfileUrl,
    string TrackingUrl,
    string? RecentPhotoUrl,
    DateTimeOffset LastSeenAt,
    string? LastSeenDescription);
