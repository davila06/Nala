namespace PawTrack.Domain.Common;

/// <summary>
/// Marker interface for aggregates that accumulate domain events during a transaction.
/// <see cref="PawTrack.Infrastructure.Persistence.PawTrackDbContext.SaveChangesAsync"/>
/// dispatches these events via MediatR after the DB commit succeeds.
/// </summary>
public interface IHasDomainEvents
{
    IReadOnlyList<object> DomainEvents { get; }
    void ClearDomainEvents();
}
