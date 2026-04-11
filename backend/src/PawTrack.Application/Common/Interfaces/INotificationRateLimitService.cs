namespace PawTrack.Application.Common.Interfaces;

/// <summary>
/// Prevents alert fatigue by enforcing a per-user, per-alert-type rate limit.
/// Implementations must be thread-safe.
/// </summary>
public interface INotificationRateLimitService
{
    /// <summary>
    /// Returns <c>true</c> if the user may receive a new notification of
    /// <paramref name="alertType"/> at this moment.
    /// </summary>
    bool IsAllowed(Guid userId, string alertType);

    /// <summary>
    /// Records that a notification of <paramref name="alertType"/> was sent to the user.
    /// Subsequent calls to <see cref="IsAllowed"/> with the same key will return
    /// <c>false</c> until the rate-limit window expires.
    /// </summary>
    void Record(Guid userId, string alertType);
}
