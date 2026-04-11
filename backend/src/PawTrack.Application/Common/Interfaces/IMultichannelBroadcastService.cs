using PawTrack.Application.Broadcast.DTOs;

namespace PawTrack.Application.Common.Interfaces;

/// <summary>
/// Orchestrates a lost-pet broadcast across all configured channels.
/// Used by <c>BroadcastLostPetCommandHandler</c>; callers never interact with
/// individual <see cref="IChannelBroadcaster"/> implementations directly.
/// </summary>
public interface IMultichannelBroadcastService
{
    /// <summary>
    /// Fans out the lost-pet alert to every enabled channel in parallel.
    /// Each channel runs independently so a failure in one does not abort others.
    /// </summary>
    /// <returns>One <see cref="BroadcastAttemptDto"/> per attempted channel.</returns>
    Task<IReadOnlyList<BroadcastAttemptDto>> BroadcastAsync(
        BroadcastMessageContext context,
        CancellationToken cancellationToken = default);
}
