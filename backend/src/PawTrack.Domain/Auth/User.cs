using System.Security.Cryptography;
using System.Text;

namespace PawTrack.Domain.Auth;

public sealed class User
{
    private readonly List<RefreshToken> _refreshTokens = [];

    private User() { } // EF Core

    public Guid Id { get; private set; }
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public UserRole Role { get; private set; }
    public bool IsEmailVerified { get; private set; }
    public string? EmailVerificationToken { get; private set; }
    public DateTimeOffset? EmailVerificationTokenExpiry { get; private set; }
    public string? PasswordResetToken { get; private set; }
    public DateTimeOffset? PasswordResetTokenExpiry { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    // ── Account lockout ───────────────────────────────────────────────────────
    public int FailedLoginAttempts { get; private set; }
    public DateTimeOffset? LockoutEnd { get; private set; }

    private const int MaxFailedAttempts = 5;
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan PasswordResetTokenLifetime = TimeSpan.FromMinutes(30);

    public bool IsLockedOut => LockoutEnd.HasValue && LockoutEnd.Value > DateTimeOffset.UtcNow;

    public IReadOnlyList<RefreshToken> RefreshTokens => _refreshTokens.AsReadOnly();

    /// <summary>
    /// Creates a new user and returns it together with the raw (plaintext) email
    /// verification token that should be embedded in the verification email.
    /// The domain entity stores only the SHA-256 hex hash of the token so that
    /// a database breach cannot be used to verify accounts without the original link.
    /// </summary>
    public static (User User, string RawToken) Create(string email, string passwordHash, string name)
    {
        var rawToken = GenerateRawToken();
        var tokenHash = ToHexHash(rawToken);

        var user = new User
        {
            Id = Guid.CreateVersion7(),
            Email = email.ToLowerInvariant(),
            PasswordHash = passwordHash,
            Name = name,
            Role = UserRole.Owner,
            IsEmailVerified = false,
            EmailVerificationToken = tokenHash,
            EmailVerificationTokenExpiry = DateTimeOffset.UtcNow.AddHours(24),
            PasswordResetToken = null,
            PasswordResetTokenExpiry = null,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        return (user, rawToken);
    }

    public string IssuePasswordResetToken()
    {
        var rawToken = GenerateRawToken();
        PasswordResetToken = ToHexHash(rawToken);
        PasswordResetTokenExpiry = DateTimeOffset.UtcNow.Add(PasswordResetTokenLifetime);
        return rawToken;
    }

    public bool ResetPassword(string rawToken, string newPasswordHash)
    {
        if (string.IsNullOrWhiteSpace(rawToken) || string.IsNullOrWhiteSpace(newPasswordHash))
            return false;

        if (PasswordResetToken is null || PasswordResetTokenExpiry is null)
            return false;

        if (PasswordResetTokenExpiry.Value < DateTimeOffset.UtcNow)
            return false;

        var hash = ToHexHash(rawToken);
        if (!string.Equals(hash, PasswordResetToken, StringComparison.Ordinal))
            return false;

        PasswordHash = newPasswordHash;
        PasswordResetToken = null;
        PasswordResetTokenExpiry = null;

        ResetFailedLogins();
        RevokeAllRefreshTokens();

        return true;
    }

    /// <summary>Computes the canonical SHA-256 hex string used for token lookup.</summary>
    public static string ToHexHash(string rawToken) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken))).ToLowerInvariant();

    private static string GenerateRawToken()
    {
        var rawTokenBytes = RandomNumberGenerator.GetBytes(32); // 256-bit CSPRNG
        return Convert.ToBase64String(rawTokenBytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('='); // URL-safe base64
    }

    /// <summary>
    /// Creates a guest (bot-originated) user account.
    /// The password hash is intentionally unusable — the account can be claimed
    /// by the real user via email verification flow after they receive the report link.
    /// </summary>
    public static User CreateGuestForBot(string syntheticEmail, string displayName) => new()
    {
        Id = Guid.CreateVersion7(),
        Email = syntheticEmail.ToLowerInvariant(),
        PasswordHash = "$2a$12$bot.account.placeholder.not.valid.bcrypt.hash.xxx", // unusable; not a real bcrypt hash
        Name = displayName,
        Role = UserRole.Owner,
        IsEmailVerified = false,
        EmailVerificationToken = null,
        EmailVerificationTokenExpiry = null,
        CreatedAt = DateTimeOffset.UtcNow,
    };

    /// <summary>
    /// Verifies the email using the raw (plaintext) token from the verification link.
    /// Hashes it internally before comparing with the stored hash.
    /// </summary>
    public bool VerifyEmail(string rawToken)
    {
        if (IsEmailVerified) return false;
        var hash = ToHexHash(rawToken);
        if (EmailVerificationToken != hash) return false;
        if (EmailVerificationTokenExpiry < DateTimeOffset.UtcNow) return false;

        IsEmailVerified = true;
        EmailVerificationToken = null;
        EmailVerificationTokenExpiry = null;
        return true;
    }

    public RefreshToken AddRefreshToken(string tokenHash, DateTimeOffset expiresAt, DateTimeOffset? sessionIssuedAt = null)
    {
        var refreshToken = RefreshToken.Create(Id, tokenHash, expiresAt, sessionIssuedAt);
        _refreshTokens.Add(refreshToken);
        return refreshToken;
    }

    public void RevokeRefreshToken(Guid tokenId)
    {
        var token = _refreshTokens.FirstOrDefault(t => t.Id == tokenId);
        token?.Revoke();
    }

    public void RevokeAllRefreshTokens()
    {
        foreach (var token in _refreshTokens.Where(t => !t.IsRevoked))
            token.Revoke();
    }

    public void PromoteToAlly()
    {
        Role = UserRole.Ally;
    }

    public void PromoteToAdmin()
    {
        Role = UserRole.Admin;
    }

    public void AssignClinicRole()
    {
        Role = UserRole.Clinic;
    }

    public void UpdateProfile(string name)
    {
        Name = name.Trim();
    }

    /// <summary>
    /// Records a failed login attempt. Locks the account after
    /// <see cref="MaxFailedAttempts"/> consecutive failures.
    /// </summary>
    public void RecordFailedLogin()
    {
        FailedLoginAttempts++;
        if (FailedLoginAttempts >= MaxFailedAttempts)
        {
            LockoutEnd = DateTimeOffset.UtcNow.Add(LockoutDuration);
        }
    }

    /// <summary>Resets failed login counter and clears any active lockout.</summary>
    public void ResetFailedLogins()
    {
        FailedLoginAttempts = 0;
        LockoutEnd = null;
    }
}
