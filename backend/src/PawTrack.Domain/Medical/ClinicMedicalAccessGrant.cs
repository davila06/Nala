using System.Security.Cryptography;
using System.Text;

namespace PawTrack.Domain.Medical;

/// <summary>
/// Explicit consent grant that allows a clinic permanent read+write access
/// to a specific pet's medical expediente.
/// Either party can initiate: owner generates a code and hands it to the clinic,
/// or the clinic generates a code and hands it to the owner.
/// The other party enters the code to activate the grant.
/// </summary>
public sealed class ClinicMedicalAccessGrant
{
    // Unambiguous uppercase alphanumeric — excludes I, L, O, 0, 1
    private const string CodeCharset = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";
    private const int CodeLength = 8;

    private ClinicMedicalAccessGrant() { } // EF Core

    public Guid Id { get; private set; }
    public Guid PetId { get; private set; }
    public Guid ClinicId { get; private set; }
    public Guid PetOwnerId { get; private set; }

    /// <summary>"Owner" or "Clinic" — who created the pending code.</summary>
    public string InitiatedBy { get; private set; } = string.Empty;

    /// <summary>SHA-256 hex of the raw 8-char code. Never expose the hash to clients.</summary>
    public string CodeHash { get; private set; } = string.Empty;

    public DateTimeOffset CodeExpiresAt { get; private set; }
    public DateTimeOffset? AcceptedAt { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    // ── Computed ─────────────────────────────────────────────────────────────

    public bool IsPending => AcceptedAt is null && DateTimeOffset.UtcNow < CodeExpiresAt;
    public bool IsCodeExpired => AcceptedAt is null && DateTimeOffset.UtcNow >= CodeExpiresAt;
    public bool IsEffectivelyActive => IsActive && AcceptedAt.HasValue;

    // ── Factory ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Creates a new pending grant and returns the raw (plaintext) code for display.
    /// The raw code must never be stored — only its hash is persisted.
    /// </summary>
    public static (ClinicMedicalAccessGrant Grant, string RawCode) Generate(
        Guid petId, Guid clinicId, Guid petOwnerId, string initiatedBy)
    {
        var rawCode = GenerateCode();
        var hash = HashCode(rawCode);
        var now = DateTimeOffset.UtcNow;

        var grant = new ClinicMedicalAccessGrant
        {
            Id = Guid.CreateVersion7(),
            PetId = petId,
            ClinicId = clinicId,
            PetOwnerId = petOwnerId,
            InitiatedBy = initiatedBy,
            CodeHash = hash,
            CodeExpiresAt = now.AddHours(24),
            AcceptedAt = null,
            IsActive = false,
            CreatedAt = now,
        };

        return (grant, rawCode);
    }

    // ── Behaviour ─────────────────────────────────────────────────────────────

    /// <summary>Validates the raw code and activates the grant if valid.</summary>
    public bool TryAccept(string rawCode)
    {
        if (AcceptedAt.HasValue || IsCodeExpired) return false;
        if (!ConstantTimeEquals(HashCode(rawCode), CodeHash)) return false;

        AcceptedAt = DateTimeOffset.UtcNow;
        IsActive = true;
        return true;
    }

    public void Revoke()
    {
        IsActive = false;
        RevokedAt = DateTimeOffset.UtcNow;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string GenerateCode()
    {
        var bytes = new byte[CodeLength];
        RandomNumberGenerator.Fill(bytes);
        var chars = new char[CodeLength];
        for (var i = 0; i < CodeLength; i++)
            chars[i] = CodeCharset[bytes[i] % CodeCharset.Length];
        return new string(chars);
    }

    private static string HashCode(string rawCode) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawCode))).ToLowerInvariant();

    private static bool ConstantTimeEquals(string a, string b)
    {
        if (a.Length != b.Length) return false;
        var diff = 0;
        for (var i = 0; i < a.Length; i++) diff |= a[i] ^ b[i];
        return diff == 0;
    }
}
