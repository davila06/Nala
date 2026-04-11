using MediatR;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Auth.Commands.Logout;

public sealed class LogoutCommandHandler(
    IRefreshTokenRepository refreshTokenRepository,
    IUserRepository userRepository,
    IJtiBlocklist jtiBlocklist,
    IUnitOfWork unitOfWork)
    : IRequestHandler<LogoutCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        var tokenHash = ComputeHash(request.RefreshToken);
        var token = await refreshTokenRepository.GetActiveByHashAsync(tokenHash, cancellationToken);

        if (token is null || token.UserId != request.UserId)
            return Result.Failure<bool>("Invalid refresh token.");

        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
            return Result.Failure<bool>("User not found.");

        user.RevokeRefreshToken(token.Id);
        userRepository.Update(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Blocklist the access token's jti so it cannot be reused within its lifetime.
        if (!string.IsNullOrEmpty(request.AccessTokenJti) && request.AccessTokenExpiresAt.HasValue)
        {
            await jtiBlocklist.AddAsync(
                request.AccessTokenJti,
                request.AccessTokenExpiresAt.Value,
                cancellationToken);
        }

        return Result.Success(true);
    }

    private static string ComputeHash(string token)
    {
        var bytes = System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
