using Microsoft.Extensions.Caching.Distributed;

namespace PawTrack.API.Services;

/// <summary>
/// Tracks "is typing" signals per chat thread.
/// Backed by <see cref="IDistributedCache"/> (Redis in production, distributed
/// in-memory cache in single-instance dev) so signals are visible across all
/// Container App instances, not just the one that received the HTTP request.
/// </summary>
public interface ITypingStateService
{
    /// <summary>Records that <paramref name="userId"/> is currently typing in <paramref name="threadId"/>.</summary>
    Task SetTypingAsync(Guid threadId, Guid userId, CancellationToken ct = default);

    /// <summary>Returns true if someone other than <paramref name="currentUserId"/> typed in the thread
    /// within the last <see cref="TypingWindowSeconds"/> seconds.</summary>
    Task<bool> IsOtherPartyTypingAsync(Guid threadId, Guid currentUserId, CancellationToken ct = default);
}

public sealed class DistributedTypingStateService(IDistributedCache cache) : ITypingStateService
{
    private const int TypingWindowSeconds = 5;

    private static readonly DistributedCacheEntryOptions CacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(TypingWindowSeconds),
    };

    // Masked chat threads are 1:1, so the last typer in the thread is enough —
    // no need to enumerate participants (IDistributedCache has no scan/prefix API).
    public Task SetTypingAsync(Guid threadId, Guid userId, CancellationToken ct = default) =>
        cache.SetStringAsync(CacheKey(threadId), userId.ToString("N"), CacheOptions, ct);

    public async Task<bool> IsOtherPartyTypingAsync(Guid threadId, Guid currentUserId, CancellationToken ct = default)
    {
        var lastTyperId = await cache.GetStringAsync(CacheKey(threadId), ct);
        return lastTyperId is not null && lastTyperId != currentUserId.ToString("N");
    }

    private static string CacheKey(Guid threadId) => $"typing:{threadId:N}";
}

