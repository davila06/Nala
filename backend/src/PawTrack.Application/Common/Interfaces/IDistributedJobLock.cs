namespace PawTrack.Application.Common.Interfaces;

/// <summary>
/// Distributed mutex for scheduled background jobs.
/// Prevents duplicate execution when multiple Container App instances start simultaneously.
/// </summary>
public interface IDistributedJobLock
{
    /// <summary>
    /// Tries to acquire an exclusive lock named <paramref name="jobName"/> for up to
    /// <paramref name="holdDuration"/>. Returns an <see cref="IAsyncDisposable"/> that
    /// releases the lock on dispose, or <c>null</c> if another instance holds it.
    /// </summary>
    Task<IAsyncDisposable?> TryAcquireAsync(
        string jobName,
        TimeSpan holdDuration,
        CancellationToken ct = default);
}
