using System.Collections.Concurrent;

namespace PawTrack.API.Services;

/// <summary>
/// Tracks "is typing" signals per chat thread in memory.
/// State is intentionally ephemeral — it does not survive process restarts,
/// and no data is persisted to the database.
/// <para>
/// ⚠️ MULTI-INSTANCE LIMITATION: In App Service scale-out deployments with
/// multiple instances, typing signals sent to instance A will not be visible
/// to clients connected to instance B. Acceptable for MVP — the 10s message
/// poll acts as fallback. Future fix: migrate to Azure SignalR Service (sticky
/// sessions + backplane) or Redis Pub/Sub.
/// </para>
/// </summary>
public interface ITypingStateService
{
    /// <summary>Records that <paramref name="userId"/> is currently typing in <paramref name="threadId"/>.</summary>
    void SetTyping(Guid threadId, Guid userId);

    /// <summary>Returns true if someone other than <paramref name="currentUserId"/> typed in the thread
    /// within the last <see cref="TypingWindowSeconds"/> seconds.</summary>
    bool IsOtherPartyTyping(Guid threadId, Guid currentUserId);
}

public sealed class InMemoryTypingStateService : ITypingStateService
{
    private const int TypingWindowSeconds = 5;
    private const int EvictionIntervalSeconds = 60;

    private readonly ConcurrentDictionary<(Guid ThreadId, Guid UserId), DateTimeOffset> _state = new();
    private DateTimeOffset _lastEviction = DateTimeOffset.UtcNow;

    public void SetTyping(Guid threadId, Guid userId)
    {
        _state[(threadId, userId)] = DateTimeOffset.UtcNow;
        EvictIfDue();
    }

    public bool IsOtherPartyTyping(Guid threadId, Guid currentUserId)
    {
        var cutoff = DateTimeOffset.UtcNow.AddSeconds(-TypingWindowSeconds);
        foreach (var kv in _state)
        {
            if (kv.Key.ThreadId == threadId &&
                kv.Key.UserId != currentUserId &&
                kv.Value > cutoff)
            {
                return true;
            }
        }
        return false;
    }

    private void EvictIfDue()
    {
        var now = DateTimeOffset.UtcNow;
        if ((now - _lastEviction).TotalSeconds < EvictionIntervalSeconds) return;
        _lastEviction = now;
        var cutoff = now.AddSeconds(-TypingWindowSeconds * 2);
        foreach (var key in _state.Keys)
            if (_state.TryGetValue(key, out var ts) && ts < cutoff)
                _state.TryRemove(key, out _);
    }
}
