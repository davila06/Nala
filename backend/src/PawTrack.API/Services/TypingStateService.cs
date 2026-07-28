using System.Collections.Concurrent;

namespace PawTrack.API.Services;

/// <summary>
/// Tracks "is typing" signals per chat thread in memory.
/// State is intentionally ephemeral — it does not survive process restarts,
/// and no data is persisted to the database.
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

    // key = (threadId, userId), value = last typing timestamp
    private readonly ConcurrentDictionary<(Guid ThreadId, Guid UserId), DateTimeOffset> _state = new();

    public void SetTyping(Guid threadId, Guid userId)
        => _state[(threadId, userId)] = DateTimeOffset.UtcNow;

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
}
