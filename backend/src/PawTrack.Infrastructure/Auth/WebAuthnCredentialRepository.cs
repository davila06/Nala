using Microsoft.EntityFrameworkCore;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Auth;
using PawTrack.Infrastructure.Persistence;

namespace PawTrack.Infrastructure.Auth;

public sealed class WebAuthnCredentialRepository(PawTrackDbContext db) : IWebAuthnCredentialRepository
{
    public async Task<IReadOnlyList<WebAuthnCredential>> GetByUserIdAsync(Guid userId, CancellationToken ct = default) =>
        await db.WebAuthnCredentials.AsTracking()
            .Where(x => x.UserId == userId && x.RevokedAt == null)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(ct);

    public Task<WebAuthnCredential?> GetByCredentialIdAsync(byte[] credentialId, CancellationToken ct = default) =>
        db.WebAuthnCredentials.FirstOrDefaultAsync(x => x.CredentialId == credentialId && x.RevokedAt == null, ct);

    public async Task AddAsync(WebAuthnCredential credential, CancellationToken ct = default) =>
        await db.WebAuthnCredentials.AddAsync(credential, ct);

    public void Update(WebAuthnCredential credential) => db.WebAuthnCredentials.Update(credential);
}