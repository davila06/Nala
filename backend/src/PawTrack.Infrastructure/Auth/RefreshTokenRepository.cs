using Microsoft.EntityFrameworkCore;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Auth;
using PawTrack.Infrastructure.Persistence;

namespace PawTrack.Infrastructure.Auth;

public sealed class RefreshTokenRepository(PawTrackDbContext dbContext) : IRefreshTokenRepository
{
    public async Task<RefreshToken?> GetActiveByHashAsync(string tokenHash, CancellationToken cancellationToken = default) =>
        await dbContext.RefreshTokens
            .AsTracking()
            .FirstOrDefaultAsync(
                rt => rt.TokenHash == tokenHash && !rt.IsRevoked && rt.ExpiresAt > DateTimeOffset.UtcNow,
                cancellationToken);

    public async Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken = default) =>
        await dbContext.RefreshTokens
            .AsTracking()
            .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash, cancellationToken);

    public async Task<IReadOnlyList<RefreshToken>> GetActiveByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
        await dbContext.RefreshTokens
            .Where(rt => rt.UserId == userId && !rt.IsRevoked && rt.ExpiresAt > DateTimeOffset.UtcNow)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(RefreshToken token, CancellationToken cancellationToken = default) =>
        await dbContext.RefreshTokens.AddAsync(token, cancellationToken);

    public void Update(RefreshToken token) =>
        dbContext.RefreshTokens.Update(token);
}
