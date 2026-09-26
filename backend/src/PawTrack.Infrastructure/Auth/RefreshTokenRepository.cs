using Microsoft.EntityFrameworkCore;
using PawTrack.Application.Auth.DTOs;
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
            .AsNoTracking()
            .Where(rt => rt.UserId == userId && !rt.IsRevoked && rt.ExpiresAt > DateTimeOffset.UtcNow)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<RefreshSessionSummaryDto>> GetActiveSessionsByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var tokens = await dbContext.RefreshTokens
            .AsNoTracking()
            .Where(token => token.UserId == userId && !token.IsRevoked && token.ExpiresAt > DateTimeOffset.UtcNow)
            .OrderByDescending(token => token.CreatedAt)
            .Select(token => new { token.SessionId, token.SessionIssuedAt, token.CreatedAt, token.ExpiresAt })
            .Take(100)
            .ToListAsync(cancellationToken);

        return tokens
            .GroupBy(token => token.SessionId)
            .Select(group => new RefreshSessionSummaryDto(
                group.Key,
                group.Min(token => token.SessionIssuedAt),
                group.Max(token => token.CreatedAt),
                group.Max(token => token.ExpiresAt)))
            .OrderByDescending(session => session.LastActivityAt)
            .Take(50)
            .ToList();
    }

    public async Task<bool> RevokeSessionAsync(Guid userId, Guid sessionId, CancellationToken cancellationToken = default)
    {
        var tokens = await dbContext.RefreshTokens
            .Where(token => token.UserId == userId && token.SessionId == sessionId && !token.IsRevoked)
            .ToListAsync(cancellationToken);
        if (tokens.Count == 0) return false;
        foreach (var token in tokens) token.Revoke();
        return true;
    }

    public async Task AddAsync(RefreshToken token, CancellationToken cancellationToken = default) =>
        await dbContext.RefreshTokens.AddAsync(token, cancellationToken);

    public void Update(RefreshToken token) =>
        dbContext.RefreshTokens.Update(token);
}
