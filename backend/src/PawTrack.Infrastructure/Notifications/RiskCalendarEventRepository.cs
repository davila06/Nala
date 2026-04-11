using Microsoft.EntityFrameworkCore;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Notifications;
using PawTrack.Infrastructure.Persistence;

namespace PawTrack.Infrastructure.Notifications;

public sealed class RiskCalendarEventRepository(PawTrackDbContext dbContext)
    : IRiskCalendarEventRepository
{
    public async Task<IReadOnlyList<RiskCalendarEvent>> GetAllActiveAsync(
        CancellationToken cancellationToken = default)
    {
        var results = await dbContext.RiskCalendarEvents
            .AsNoTracking()
            .Where(e => e.IsActive)
            .OrderBy(e => e.Month)
            .ThenBy(e => e.Day)
            .ToListAsync(cancellationToken);

        return results.AsReadOnly();
    }

    public async Task<IReadOnlyList<RiskCalendarEvent>> GetByTriggerDateAsync(
        DateOnly triggerDate,
        CancellationToken cancellationToken = default)
    {
        // Filter active events whose alert trigger date (Event.Day/Month minus DaysBeforeAlert)
        // matches the requested date. The comparison is done in-memory after a coarse DB filter
        // to avoid per-row function calls that would block index usage.
        var events = await dbContext.RiskCalendarEvents
            .AsNoTracking()
            .Where(e => e.IsActive)
            .ToListAsync(cancellationToken);

        var matches = events
            .Where(e =>
            {
                try { return e.AlertTriggerDate(triggerDate.Year) == triggerDate; }
                catch (ArgumentOutOfRangeException) { return false; } // e.g. Feb 30
            })
            .ToList();

        return matches.AsReadOnly();
    }

    public async Task AddAsync(RiskCalendarEvent riskEvent, CancellationToken cancellationToken = default)
        => await dbContext.RiskCalendarEvents.AddAsync(riskEvent, cancellationToken);

    public Task<bool> AnyAsync(CancellationToken cancellationToken = default)
        => dbContext.RiskCalendarEvents.AnyAsync(cancellationToken);
}
