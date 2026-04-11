using System.Collections.Concurrent;
using PawTrack.Application.Common.Interfaces;

namespace PawTrack.Infrastructure.Auth;

/// <summary>
/// In-process jti blocklist backed by a <see cref="ConcurrentDictionary"/>.
/// Suitable for single-instance deployments.  For multi-instance scale-out,
/// replace with a Redis-backed implementation.
/// Entries are pruned lazily on every <see cref="IsBlockedAsync"/> lookup and
/// eagerly rejected once their expiry has passed.
/// </summary>
public sealed class InMemoryJtiBlocklist : IJtiBlocklist
{
    // Key = jti, Value = absolute expiry instant
    private readonly ConcurrentDictionary<string, DateTimeOffset> _store = new();

    public Task AddAsync(string jti, DateTimeOffset expiresAt, CancellationToken cancellationToken)
    {
        _store[jti] = expiresAt;
        return Task.CompletedTask;
    }

    public Task<bool> IsBlockedAsync(string jti, CancellationToken cancellationToken)
    {
        if (!_store.TryGetValue(jti, out var expiresAt))
            return Task.FromResult(false);

        if (DateTimeOffset.UtcNow > expiresAt)
        {
            // Lazy eviction — the entry has expired; remove it.
            _store.TryRemove(jti, out _);
            return Task.FromResult(false);
        }

        return Task.FromResult(true);
    }
}
