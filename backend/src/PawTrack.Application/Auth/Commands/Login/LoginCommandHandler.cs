using MediatR;
using Microsoft.Extensions.Logging;
using PawTrack.Application.Auth.DTOs;
using PawTrack.Application.Common;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Auth.Commands.Login;

public sealed class LoginCommandHandler(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    IJwtTokenService jwtTokenService,
    IUnitOfWork unitOfWork,
    ILogger<LoginCommandHandler> logger)
    : IRequestHandler<LoginCommand, Result<AuthTokenDto>>
{
    private static readonly int RefreshTokenExpiryDays = 30;

    public async Task<Result<AuthTokenDto>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByEmailAsync(request.Email, cancellationToken);

        // Use a constant-time-equivalent path: always hash-verify even when user not found
        // to prevent user enumeration via timing differences.
        if (user is null)
        {
            passwordHasher.Verify(request.Password, "$2a$12$placeholder.hash.to.prevent.timing.attack.xxxxxxxxxx");
            logger.LogWarning("Auth.Login.UserNotFound EmailHash={EmailHash}", PiiHelper.MaskEmail(request.Email));
            return Result.Failure<AuthTokenDto>("Invalid email or password.");
        }

        // Check lockout before verifying password to prevent brute-force even after lockout expires.
        if (user.IsLockedOut)
        {
            logger.LogWarning("Auth.Login.AccountLocked UserId={UserId} LockoutEnd={LockoutEnd}",
                user.Id, user.LockoutEnd);
            return Result.Failure<AuthTokenDto>(
                "This account has been temporarily locked due to too many failed login attempts. Please try again later.");
        }

        if (!passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            user.RecordFailedLogin();
            userRepository.Update(user);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            logger.LogWarning(
                "Auth.Login.InvalidPassword UserId={UserId} FailedAttempts={FailedAttempts} LockedOut={LockedOut}",
                user.Id, user.FailedLoginAttempts, user.IsLockedOut);

            return Result.Failure<AuthTokenDto>("Invalid email or password.");
        }

        if (!user.IsEmailVerified)
        {
            logger.LogWarning("Auth.Login.EmailNotVerified UserId={UserId}", user.Id);
            return Result.Failure<AuthTokenDto>("Email address not yet verified. Please check your inbox.");
        }

        // Successful login — reset lockout counter.
        user.ResetFailedLogins();

        var (rawToken, tokenHash) = jwtTokenService.GenerateRefreshToken();
        var expiresAt = DateTimeOffset.UtcNow.AddDays(RefreshTokenExpiryDays);
        var refreshToken = user.AddRefreshToken(tokenHash, expiresAt);

        userRepository.Update(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var accessToken = jwtTokenService.GenerateAccessToken(user.Id, user.Email, user.Name, user.Role);

        logger.LogInformation("Auth.Login.Success UserId={UserId} Role={Role}", user.Id, user.Role);

        return Result.Success(new AuthTokenDto(
            AccessToken: accessToken,
            RefreshToken: rawToken,
            ExpiresIn: jwtTokenService.AccessTokenExpirySeconds,
            User: UserProfileDto.FromDomain(user)));
    }
}
