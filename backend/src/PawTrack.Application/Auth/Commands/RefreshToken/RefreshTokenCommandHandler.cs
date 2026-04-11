using MediatR;
using Microsoft.Extensions.Logging;
using PawTrack.Application.Auth.DTOs;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Auth.Commands.RefreshToken;

public sealed class RefreshTokenCommandHandler(
    IRefreshTokenRepository refreshTokenRepository,
    IUserRepository userRepository,
    IJwtTokenService jwtTokenService,
    IUnitOfWork unitOfWork,
    ILogger<RefreshTokenCommandHandler> logger)
    : IRequestHandler<RefreshTokenCommand, Result<AuthTokenDto>>
{
    private static readonly int RefreshTokenExpiryDays = 30;

    /// <summary>
    /// Maximum lifetime for any session regardless of how many times the token is rotated.
    /// A session started at <c>SessionIssuedAt</c> must not be refreshable after
    /// <c>SessionIssuedAt + AbsoluteSessionMaxDays</c> days, even if the rolling 30-day
    /// window hasn't expired yet.
    /// </summary>
    private static readonly int AbsoluteSessionMaxDays = 90;

    public async Task<Result<AuthTokenDto>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var tokenHash = ComputeHash(request.Token);

        // Look up the token regardless of revocation status so we can detect
        // token theft: if a previously-rotated (revoked) token is replayed,
        // the session is considered compromised and all sessions are invalidated.
        var existing = await refreshTokenRepository.GetByHashAsync(tokenHash, cancellationToken);

        if (existing is null)
        {
            logger.LogWarning("Auth.Refresh.TokenNotFound");
            return Result.Failure<AuthTokenDto>("Invalid or expired refresh token.");
        }

        // ── Token theft detection ─────────────────────────────────────────────
        // A revoked token being replayed means an attacker stole a previously
        // issued token. Revoke every active session for this user immediately.
        if (existing.IsRevoked)
        {
            var compromisedUser = await userRepository.GetByIdAsync(existing.UserId, cancellationToken);
            if (compromisedUser is not null)
            {
                compromisedUser.RevokeAllRefreshTokens();
                userRepository.Update(compromisedUser);
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }

            // SECURITY ALERT: structured log for SIEM correlation
            logger.LogCritical(
                "Auth.Refresh.TokenTheftDetected UserId={UserId} — all sessions revoked",
                existing.UserId);

            return Result.Failure<AuthTokenDto>("Invalid or expired refresh token.");
        }

        if (existing.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            logger.LogWarning("Auth.Refresh.TokenExpired UserId={UserId}", existing.UserId);
            return Result.Failure<AuthTokenDto>("Invalid or expired refresh token.");
        }

        // ── Absolute session expiry check ──────────────────────────────────────
        // The session birth date is carried on every token in the rotation chain.
        // If the original session is older than AbsoluteSessionMaxDays, reject
        // even if the rolling 30-day window hasn't lapsed yet.
        // This caps the worst-case exposure from a silently-stolen refresh token.
        var absoluteDeadline = existing.SessionIssuedAt.AddDays(AbsoluteSessionMaxDays);
        if (DateTimeOffset.UtcNow >= absoluteDeadline)
        {
            logger.LogWarning(
                "Auth.Refresh.AbsoluteSessionExpired UserId={UserId} SessionIssuedAt={SessionIssuedAt}",
                existing.UserId, existing.SessionIssuedAt);
            return Result.Failure<AuthTokenDto>("Invalid or expired refresh token.");
        }

        var user = await userRepository.GetByIdAsync(existing.UserId, cancellationToken);
        if (user is null)
            return Result.Failure<AuthTokenDto>("User not found.");

        // Rotate: revoke old, issue new — cap expiry at the absolute session deadline
        user.RevokeRefreshToken(existing.Id);

        var (rawToken, newHash) = jwtTokenService.GenerateRefreshToken();
        var rollingExpiry   = DateTimeOffset.UtcNow.AddDays(RefreshTokenExpiryDays);
        var expiresAt       = rollingExpiry < absoluteDeadline ? rollingExpiry : absoluteDeadline;
        user.AddRefreshToken(newHash, expiresAt, sessionIssuedAt: existing.SessionIssuedAt);

        userRepository.Update(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var accessToken = jwtTokenService.GenerateAccessToken(user.Id, user.Email, user.Name, user.Role);

        logger.LogInformation("Auth.Refresh.Success UserId={UserId}", user.Id);

        return Result.Success(new AuthTokenDto(
            AccessToken: accessToken,
            RefreshToken: rawToken,
            ExpiresIn: jwtTokenService.AccessTokenExpirySeconds,
            User: UserProfileDto.FromDomain(user)));
    }

    private static string ComputeHash(string token)
    {
        var bytes = System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
