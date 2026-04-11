using Microsoft.Extensions.Caching.Memory;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Locations;

namespace PawTrack.Infrastructure.Notifications;

/// <summary>
/// In-memory rate-limit gate backed by <see cref="IMemoryCache"/>.
/// Thread-safe because <see cref="IMemoryCache"/> is designed for concurrent access.
/// </summary>
public sealed class MemoryCacheNotificationRateLimitService(IMemoryCache cache)
    : INotificationRateLimitService
{
    public bool IsAllowed(Guid userId, string alertType) =>
        !cache.TryGetValue(CacheKey(userId, alertType), out _);

    public void Record(Guid userId, string alertType) =>
        cache.Set(
            CacheKey(userId, alertType),
            true,
            TimeSpan.FromMinutes(GeofenceConstants.RateLimitWindowMinutes));

    private static string CacheKey(Guid userId, string alertType) =>
        $"ratelimit:{alertType}:{userId:N}";
}
