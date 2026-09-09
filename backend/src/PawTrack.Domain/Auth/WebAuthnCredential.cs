namespace PawTrack.Domain.Auth;

public sealed class WebAuthnCredential
{
    private WebAuthnCredential() { }

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public byte[] CredentialId { get; private set; } = [];
    public byte[] PublicKey { get; private set; } = [];
    public uint SignatureCounter { get; private set; }
    public string? DeviceName { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? LastUsedAt { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }

    public bool IsActive => RevokedAt is null;

    public static WebAuthnCredential Create(
        Guid userId,
        byte[] credentialId,
        byte[] publicKey,
        uint signatureCounter,
        string? deviceName = null) => new()
        {
            Id = Guid.CreateVersion7(),
            UserId = userId,
            CredentialId = credentialId.ToArray(),
            PublicKey = publicKey.ToArray(),
            SignatureCounter = signatureCounter,
            DeviceName = string.IsNullOrWhiteSpace(deviceName) ? null : deviceName.Trim()[..Math.Min(deviceName.Trim().Length, 120)],
            CreatedAt = DateTimeOffset.UtcNow,
        };

    public bool UpdateCounter(uint counter)
    {
        if (!IsActive || counter < SignatureCounter) return false;
        SignatureCounter = counter;
        LastUsedAt = DateTimeOffset.UtcNow;
        return true;
    }

    public void Revoke() => RevokedAt ??= DateTimeOffset.UtcNow;
}