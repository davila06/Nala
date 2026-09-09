using PawTrack.Domain.Auth;

namespace PawTrack.Application.Common.Interfaces;

public interface IWebAuthnCredentialRepository
{
    Task<IReadOnlyList<WebAuthnCredential>> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<WebAuthnCredential?> GetByCredentialIdAsync(byte[] credentialId, CancellationToken ct = default);
    Task AddAsync(WebAuthnCredential credential, CancellationToken ct = default);
    void Update(WebAuthnCredential credential);
}