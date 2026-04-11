using PawTrack.Domain.Notifications;

namespace PawTrack.Application.Common.Interfaces;

public interface IRiskCalendarEventRepository
{
    Task<IReadOnlyList<RiskCalendarEvent>> GetAllActiveAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RiskCalendarEvent>> GetByTriggerDateAsync(DateOnly triggerDate, CancellationToken cancellationToken = default);
    Task AddAsync(RiskCalendarEvent riskEvent, CancellationToken cancellationToken = default);
    Task<bool> AnyAsync(CancellationToken cancellationToken = default);
}
