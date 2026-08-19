using Microsoft.EntityFrameworkCore;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Auth;
using PawTrack.Infrastructure.Persistence;

namespace PawTrack.Infrastructure.Auth;

/// <summary>
/// SQL-backed JTI blocklist — survives restarts and works across all App Service instances.
/// Replaces the in-memory singleton that was per-process and invisible to sibling instances.
/// </summary>
public sealed class DbJtiBlocklist(PawTrackDbContext db) : IJtiBlocklist
{
    public async Task AddAsync(string jti, DateTimeOffset expiresAt, CancellationToken cancellationToken)
    {
        // Upsert-safe: if the jti was added by a race, silently ignore the duplicate key
        if (!await db.RevokedTokens.AnyAsync(r => r.Jti == jti, cancellationToken))
        {
            db.RevokedTokens.Add(RevokedToken.Create(jti, expiresAt));
            try { await db.SaveChangesAsync(cancellationToken); }
            catch (DbUpdateException) { /* duplicate — another instance won the race */ }
        }
    }

    public async Task<bool> IsBlockedAsync(string jti, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        return await db.RevokedTokens
            .AnyAsync(r => r.Jti == jti && r.ExpiresAt > now, cancellationToken);
    }
}
