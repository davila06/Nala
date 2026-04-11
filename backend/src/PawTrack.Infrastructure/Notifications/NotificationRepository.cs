using Microsoft.EntityFrameworkCore;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Notifications;
using PawTrack.Infrastructure.Persistence;

namespace PawTrack.Infrastructure.Notifications;

public sealed class NotificationRepository(PawTrackDbContext dbContext) : INotificationRepository
{
    public async Task<IReadOnlyList<Notification>> GetByUserIdAsync(
        Guid userId, int skip, int take, CancellationToken cancellationToken = default) =>
        await dbContext.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Skip(skip)
            .Take(take)
            .AsTracking()
            .ToListAsync(cancellationToken);

    public async Task<int> CountUnreadAsync(Guid userId, CancellationToken cancellationToken = default) =>
        await dbContext.Notifications
            .CountAsync(n => n.UserId == userId && !n.IsRead, cancellationToken);

    public async Task<Notification?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await dbContext.Notifications
            .AsTracking()
            .FirstOrDefaultAsync(n => n.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Notification>> GetUnreadByUserIdAsync(
        Guid userId, CancellationToken cancellationToken = default) =>
        await dbContext.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .AsTracking()
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<Notification>> GetByLostEventIdAsync(
        Guid lostPetEventId, CancellationToken cancellationToken = default)
    {
        var idString = lostPetEventId.ToString();
        var results = await dbContext.Notifications
            .Where(n => n.RelatedEntityId == idString)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync(cancellationToken);

        return results.AsReadOnly();
    }

    public async Task<IReadOnlyList<Notification>> GetByUserIdAndTypeAsync(
        Guid userId,
        NotificationType type,
        CancellationToken cancellationToken = default)
    {
        var results = await dbContext.Notifications
            .Where(n => n.UserId == userId && n.Type == type)
            .OrderByDescending(n => n.CreatedAt)
            .AsTracking()
            .ToListAsync(cancellationToken);

        return results.AsReadOnly();
    }

    public async Task<bool> HasRecentByUserTypeAndEntityAsync(
        Guid userId,
        NotificationType type,
        string relatedEntityId,
        TimeSpan within,
        CancellationToken cancellationToken = default)
    {
        var threshold = DateTimeOffset.UtcNow.Subtract(within);

        return await dbContext.Notifications
            .AnyAsync(n =>
                    n.UserId == userId
                    && n.Type == type
                    && n.RelatedEntityId == relatedEntityId
                    && n.CreatedAt >= threshold,
                cancellationToken);
    }

    public async Task AddAsync(Notification notification, CancellationToken cancellationToken = default) =>
        await dbContext.Notifications.AddAsync(notification, cancellationToken);

    public void Update(Notification notification) =>
        dbContext.Notifications.Update(notification);
}
