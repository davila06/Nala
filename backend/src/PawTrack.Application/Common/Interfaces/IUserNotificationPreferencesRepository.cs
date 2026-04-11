using PawTrack.Domain.Notifications;

namespace PawTrack.Application.Common.Interfaces;

public interface IUserNotificationPreferencesRepository
{
    Task<UserNotificationPreferences?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task AddAsync(UserNotificationPreferences prefs, CancellationToken cancellationToken = default);
    void Update(UserNotificationPreferences prefs);

    /// <summary>
    /// Returns all user IDs that have preventive alerts enabled (or have no record — defaults to enabled).
    /// </summary>
    Task<IReadOnlyList<Guid>> GetUserIdsWithPreventiveAlertsEnabledAsync(CancellationToken cancellationToken = default);
}
