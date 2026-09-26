using PawTrack.Domain.Auth;

namespace PawTrack.Application.Common.Interfaces;

public interface IJwtTokenService
{
    string GenerateAccessToken(Guid userId, string email, string name, UserRole role, bool mfaVerified = false, Guid? sessionId = null);
    (string rawToken, string hash) GenerateRefreshToken();
    int AccessTokenExpirySeconds { get; }
}
