using PawTrack.Domain.Auth;

namespace PawTrack.Application.Common.Interfaces;

public interface IJwtTokenService
{
    string GenerateAccessToken(Guid userId, string email, string name, UserRole role);
    (string rawToken, string hash) GenerateRefreshToken();
    int AccessTokenExpirySeconds { get; }
}
