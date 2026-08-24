using Microsoft.Extensions.Caching.Distributed;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Locations;

namespace PawTrack.Infrastructure.Notifications;

/// <summary>
/// Distributed rate-limit gate backed by <see cref="IDistributedCache"/>.
/// Uses Redis (production) or in-memory distributed cache (development) via DI registration.
/// Safe across multiple Container App instances — Redis keys are shared.
/// </summary>
public sealed class DistributedNotificationRateLimitService(IDistributedCache cache)
    : INotificationRateLimitService
{
    private static readonly DistributedCacheEntryOptions CacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(GeofenceConstants.RateLimitWindowMinutes),
    };

    public bool IsAllowed(Guid userId, string alertType)
    {
        var bytes = cache.Get(CacheKey(userId, alertType));
        return bytes is null;
    }

    public void Record(Guid userId, string alertType) =>
        cache.Set(CacheKey(userId, alertType), [1], CacheOptions);

    private static string CacheKey(Guid userId, string alertType) =>
        $"ratelimit:{alertType}:{userId:N}";
}
