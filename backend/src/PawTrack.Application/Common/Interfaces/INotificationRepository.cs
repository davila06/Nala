using PawTrack.Domain.Notifications;

namespace PawTrack.Application.Common.Interfaces;

public interface INotificationRepository
{
    Task<IReadOnlyList<Notification>> GetByUserIdAsync(
        Guid userId, int skip, int take, CancellationToken cancellationToken = default);

    Task<int> CountUnreadAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<Notification?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Notification>> GetUnreadByUserIdAsync(
        Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns all notifications whose <see cref="Notification.RelatedEntityId"/> equals
    /// the string representation of <paramref name="lostPetEventId"/>, ordered by
    /// <see cref="Notification.CreatedAt"/> descending.
    /// Used by the Case Room to display the anonymised nearby-alert dispatch log.
    /// </summary>
    Task<IReadOnlyList<Notification>> GetByLostEventIdAsync(
        Guid lostPetEventId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Notification>> GetByUserIdAndTypeAsync(
        Guid userId,
        NotificationType type,
        CancellationToken cancellationToken = default);

    Task<bool> HasRecentByUserTypeAndEntityAsync(
        Guid userId,
        NotificationType type,
        string relatedEntityId,
        TimeSpan within,
        CancellationToken cancellationToken = default);

    Task AddAsync(Notification notification, CancellationToken cancellationToken = default);
    void Update(Notification notification);
}
