namespace PawTrack.Application.Common.Interfaces;

/// <summary>
/// Generates short, trackable URLs that redirect to the pet profile.
/// Allows the system to measure clicks per channel without any external
/// URL-shortening dependency (e.g. bit.ly).
/// </summary>
public interface ITrackingLinkService
{
    /// <summary>
    /// Creates (or retrieves) a tracking URL for <paramref name="lostPetEventId"/>
    /// on the given <paramref name="channel"/>.
    /// Format: {baseUrl}/t/{code}?ch={channel}
    /// The code is deterministic (hash of eventId + channel) so repeated calls
    /// for the same inputs return the same URL — safe for idempotent handlers.
    /// </summary>
    string Generate(Guid lostPetEventId, string channel);
}
