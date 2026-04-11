using PawTrack.Application.Common.Interfaces;

namespace PawTrack.Infrastructure.Auth;

/// <summary>
/// BCrypt-based password hasher. Uses work factor 12 (OWASP recommended for 2026).
/// </summary>
public sealed class PasswordHasher : IPasswordHasher
{
    private const int WorkFactor = 12;

    public string Hash(string password) =>
        BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);

    public bool Verify(string password, string hash) =>
        BCrypt.Net.BCrypt.Verify(password, hash);
}
