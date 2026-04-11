using Microsoft.EntityFrameworkCore;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Notifications;
using PawTrack.Infrastructure.Persistence;

namespace PawTrack.Infrastructure.Notifications;

public sealed class UserNotificationPreferencesRepository(PawTrackDbContext dbContext)
    : IUserNotificationPreferencesRepository
{
    public async Task<UserNotificationPreferences?> GetByUserIdAsync(
        Guid userId, CancellationToken cancellationToken = default)
        => await dbContext.UserNotificationPreferences
            .AsTracking()
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);

    public async Task AddAsync(
        UserNotificationPreferences prefs, CancellationToken cancellationToken = default)
        => await dbContext.UserNotificationPreferences.AddAsync(prefs, cancellationToken);

    public void Update(UserNotificationPreferences prefs)
        => dbContext.UserNotificationPreferences.Update(prefs);

    public async Task<IReadOnlyList<Guid>> GetUserIdsWithPreventiveAlertsEnabledAsync(
        CancellationToken cancellationToken = default)
    {
        // Users with an explicit opt-out record
        var optedOut = await dbContext.UserNotificationPreferences
            .AsNoTracking()
            .Where(p => !p.EnablePreventiveAlerts)
            .Select(p => p.UserId)
            .ToHashSetAsync(cancellationToken);

        // All user IDs that have at least one active pet
        var eligible = await (
            from u in dbContext.Users
            where dbContext.Pets.Any(p => p.OwnerId == u.Id)
            select u.Id)
            .ToListAsync(cancellationToken);

        // Return eligible users who have NOT opted out
        var result = eligible.Where(id => !optedOut.Contains(id)).ToList();
        return result.AsReadOnly();
    }
}
