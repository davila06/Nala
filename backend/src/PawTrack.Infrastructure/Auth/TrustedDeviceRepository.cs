using Microsoft.EntityFrameworkCore;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Auth;
using PawTrack.Infrastructure.Persistence;

namespace PawTrack.Infrastructure.Auth;

public sealed class TrustedDeviceRepository(PawTrackDbContext dbContext) : ITrustedDeviceRepository
{
    public async Task<TrustedDevice?> GetByTokenHashAsync(Guid userId, string tokenHash, CancellationToken cancellationToken = default) =>
        await dbContext.TrustedDevices
            .AsTracking()
            .FirstOrDefaultAsync(device => device.UserId == userId && device.TokenHash == tokenHash, cancellationToken);

    public async Task<TrustedDevice?> GetByIdAsync(Guid userId, Guid deviceId, CancellationToken cancellationToken = default) =>
        await dbContext.TrustedDevices
            .AsTracking()
            .FirstOrDefaultAsync(device => device.UserId == userId && device.Id == deviceId, cancellationToken);

    public async Task<IReadOnlyList<TrustedDevice>> GetActiveByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
        await dbContext.TrustedDevices
            .AsNoTracking()
            .Where(device => device.UserId == userId && device.RevokedAt == null && device.ExpiresAt > DateTimeOffset.UtcNow)
            .OrderByDescending(device => device.LastUsedAt)
            .Take(50)
            .ToListAsync(cancellationToken);

    public async Task RevokeForSessionAsync(Guid userId, Guid sessionId, CancellationToken cancellationToken = default)
    {
        var devices = await dbContext.TrustedDevices
            .AsTracking()
            .Where(candidate => candidate.UserId == userId
                && candidate.LastSessionId == sessionId
                && candidate.RevokedAt == null)
            .ToListAsync(cancellationToken);
        foreach (var device in devices) device.Revoke();
    }

    public async Task RevokeAllForUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var devices = await dbContext.TrustedDevices
            .Where(device => device.UserId == userId && device.RevokedAt == null)
            .ToListAsync(cancellationToken);
        foreach (var device in devices) device.Revoke();
    }

    public async Task AddAsync(TrustedDevice device, CancellationToken cancellationToken = default) =>
        await dbContext.TrustedDevices.AddAsync(device, cancellationToken);

    public void Update(TrustedDevice device) => dbContext.TrustedDevices.Update(device);
}
