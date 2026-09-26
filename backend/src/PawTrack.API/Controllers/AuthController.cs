using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Hosting;
using PawTrack.Application.Auth.Commands.ChangePassword;
using PawTrack.Application.Auth.Commands.DeleteAccount;
using PawTrack.Application.Auth.Commands.ForgotPassword;
using PawTrack.Application.Auth.Commands.GrantHealthDataConsent;
using PawTrack.Application.Auth.Commands.Login;
using PawTrack.Application.Auth.Commands.Logout;
using PawTrack.Application.Auth.Commands.RefreshToken;
using PawTrack.Application.Auth.Commands.Register;
using PawTrack.Application.Auth.Commands.ResetPassword;
using PawTrack.Application.Auth.Commands.UpdateUserProfile;
using PawTrack.Application.Auth.Commands.VerifyEmail;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Auth.Queries.ExportMyData;
using PawTrack.Application.Auth.Queries.GetMyProfile;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace PawTrack.API.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(
    ISender sender,
    IHostEnvironment environment,
    IUserRepository userRepository,
    IMfaService mfaService,
    IUnitOfWork unitOfWork,
    IRefreshTokenRepository refreshTokenRepository,
    ITrustedDeviceRepository trustedDeviceRepository,
    IJtiBlocklist jtiBlocklist,
    IJwtTokenService jwtTokenService) : ControllerBase
{
    [HttpPost("register")]
    [EnableRateLimiting("register")]
    [RequestSizeLimit(1024)] // Name + Email + Password — max realistic payload ~600 B; 1 KB ceiling
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Register(
        [FromBody] RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new RegisterCommand(request.Name, request.Email, request.Password, request.IsAdultConfirmed),
            cancellationToken);

        if (result.IsFailure)
            return BadRequest(new ProblemDetails { Title = "Registration failed", Detail = string.Join("; ", result.Errors), Status = 400 });

        // Anti-enumeration: identical 201 response regardless of whether the
        // email was already registered. Users must check their inbox.
        return Created(string.Empty, new { message = "If this email is not registered, a verification link has been sent." });
    }

    [HttpGet("verify-email")]
    [EnableRateLimiting("verify-email")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> VerifyEmail(
        [FromQuery] string token,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new VerifyEmailCommand(token), cancellationToken);

        if (result.IsFailure)
            return BadRequest(new ProblemDetails { Title = "Verification failed", Detail = string.Join("; ", result.Errors), Status = 400 });

        return Ok(new { message = "Email verified successfully." });
    }

    [HttpPost("login")]
    [EnableRateLimiting("login")]
    [RequestSizeLimit(512)] // Email + Password — max realistic payload ~400 B; 512 B ceiling
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        Request.Cookies.TryGetValue("trustedDevice", out var trustedDeviceToken);
        var result = await sender.Send(new LoginCommand(request.Email, request.Password, request.MfaCode, trustedDeviceToken), cancellationToken);

        if (result.IsFailure)
            return Unauthorized(new ProblemDetails { Title = "Authentication failed", Detail = string.Join("; ", result.Errors), Status = 401 });

        var token = result.Value!;

        if (!string.IsNullOrWhiteSpace(token.RenewedTrustedDeviceToken))
            Response.Cookies.Append("trustedDevice", token.RenewedTrustedDeviceToken, TrustedDeviceCookieOptions());

        // Refresh token in HttpOnly cookie.
        // SameSite=Lax: safer than None, compatible with OAuth redirect flows
        // that land back on this origin. Strict would drop the cookie on cross-site
        // redirects (e.g., social login callbacks).
        Response.Cookies.Append("refreshToken", token.RefreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = !environment.IsDevelopment(),
            SameSite = SameSiteMode.Lax,
            Expires = DateTimeOffset.UtcNow.AddDays(30),
            // Scope to /api/auth so the cookie is NOT sent to /api/pets, /api/found-pets, etc.
            // Only refresh, logout (both under /api/auth) need it.
            Path = "/api/auth",
        });

        return Ok(new
        {
            accessToken = token.AccessToken,
            expiresIn = token.ExpiresIn,
            user = token.User,
        });
    }

    [HttpPost("mfa/setup")]
    [Authorize]
    [EnableRateLimiting("public-api")]
    public async Task<IActionResult> SetupMfa(CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var user = await userRepository.GetByIdAsync(userId, ct);
        if (user is null) return Unauthorized();
        var setup = mfaService.CreateSetup(user.Email);
        return Ok(new { secret = setup.Secret, otpauthUri = setup.OtpAuthUri });
    }

    [HttpPost("mfa/enable")]
    [Authorize]
    [EnableRateLimiting("handover-verify")]
    public async Task<IActionResult> EnableMfa([FromBody] EnableMfaRequest request, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var user = await userRepository.GetByIdAsync(userId, ct);
        if (user is null) return Unauthorized();
        var protectedSecret = mfaService.ProtectSecret(request.Secret);
        if (!mfaService.Verify(protectedSecret, request.Code))
            return UnprocessableEntity(new ProblemDetails { Detail = "El código MFA no es válido.", Status = 422 });
        var recoveryCodes = user.ConfigureMfa(protectedSecret);
        userRepository.Update(user);
        await unitOfWork.SaveChangesAsync(ct);
        return Ok(new { recoveryCodes });
    }

    [HttpPost("mfa/step-up")]
    [Authorize]
    [EnableRateLimiting("mfa-step-up")]
    [RequestSizeLimit(256)]
    public async Task<IActionResult> MfaStepUp([FromBody] MfaStepUpRequest request, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        if (!TryGetSessionId(out var sessionId))
            return BadRequest(new ProblemDetails { Detail = "La sesión actual no tiene identificador.", Status = 400 });
        var user = await userRepository.GetByIdAsync(userId, ct);
        if (user is null) return Unauthorized();
        if (!user.HasMfa)
            return UnprocessableEntity(new ProblemDetails { Detail = "MFA no está configurado.", Status = 422 });

        var validTotp = mfaService.Verify(user.MfaSecretProtected!, request.Code);
        var validRecoveryCode = !validTotp && user.ConsumeMfaRecoveryCode(request.Code);
        if (!validTotp && !validRecoveryCode)
            return UnprocessableEntity(new ProblemDetails { Detail = "El código MFA no es válido.", Status = 422 });

        if (validRecoveryCode)
        {
            userRepository.Update(user);
            await unitOfWork.SaveChangesAsync(ct);
        }

        var elevatedToken = jwtTokenService.GenerateAccessToken(
            user.Id, user.Email, user.Name, user.Role,
            mfaVerified: true,
            sessionId: sessionId);
        return Ok(new { accessToken = elevatedToken, expiresIn = jwtTokenService.AccessTokenExpirySeconds });
    }

    [HttpDelete("mfa")]
    [Authorize(Policy = "MfaStepUp")]
    [EnableRateLimiting("change-password")]
    public async Task<IActionResult> DisableMfa(CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var user = await userRepository.GetByIdAsync(userId, ct);
        if (user is null) return Unauthorized();
        var activeSessions = await refreshTokenRepository.GetActiveSessionsByUserIdAsync(userId, ct);
        user.DisableMfa();
        user.RevokeAllRefreshTokens();
        await trustedDeviceRepository.RevokeAllForUserAsync(userId, ct);
        userRepository.Update(user);
        await unitOfWork.SaveChangesAsync(ct);
        foreach (var session in activeSessions)
            await jtiBlocklist.AddAsync($"session:{session.SessionId:N}", DateTimeOffset.UtcNow.AddDays(90), ct);
        Response.Cookies.Delete("refreshToken", new CookieOptions { Path = "/api/auth" });
        Response.Cookies.Delete("trustedDevice", new CookieOptions { Path = "/api/auth" });
        return NoContent();
    }

    [HttpPost("forgot-password")]
    [EnableRateLimiting("forgot-password")]
    [RequestSizeLimit(512)] // Email-only payload
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ForgotPassword(
        [FromBody] ForgotPasswordRequest request,
        CancellationToken cancellationToken)
    {
        await sender.Send(new ForgotPasswordCommand(request.Email), cancellationToken);

        // Anti-enumeration: accepted regardless of account existence.
        return Accepted(new { message = "If the email exists, a reset link has been sent." });
    }

    [HttpPost("reset-password")]
    [EnableRateLimiting("reset-password")]
    [RequestSizeLimit(1024)] // Token + new password
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ResetPassword(
        [FromBody] ResetPasswordRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new ResetPasswordCommand(request.Token, request.NewPassword),
            cancellationToken);

        if (result.IsFailure)
            return BadRequest(new ProblemDetails
            {
                Title = "Password reset failed",
                Detail = string.Join("; ", result.Errors),
                Status = 400,
            });

        return Ok(new { message = "Password updated successfully." });
    }

    [HttpPost("refresh")]
    [EnableRateLimiting("refresh")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh(CancellationToken cancellationToken)
    {
        if (!Request.Cookies.TryGetValue("refreshToken", out var rawToken) || string.IsNullOrEmpty(rawToken))
            return Unauthorized(new ProblemDetails { Title = "No refresh token", Status = 401 });

        var result = await sender.Send(new RefreshTokenCommand(rawToken), cancellationToken);

        if (result.IsFailure)
        {
            Response.Cookies.Delete("refreshToken", new CookieOptions { Path = "/api/auth" });
            return Unauthorized(new ProblemDetails { Title = "Token refresh failed", Detail = string.Join("; ", result.Errors), Status = 401 });
        }

        var token = result.Value!;

        Response.Cookies.Append("refreshToken", token.RefreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = !environment.IsDevelopment(),
            SameSite = SameSiteMode.Strict,
            Expires = DateTimeOffset.UtcNow.AddDays(30),
            Path = "/api/auth",
        });

        return Ok(new
        {
            accessToken = token.AccessToken,
            expiresIn = token.ExpiresIn,
            user = token.User,
        });
    }

    [HttpPost("logout")]
    [Authorize]
    [EnableRateLimiting("public-api")] // 30/min — each call writes to the JTI blocklist + DB; flood evicts legit revocations
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        if (userIdClaim is null || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        if (!Request.Cookies.TryGetValue("refreshToken", out var rawToken) || string.IsNullOrEmpty(rawToken))
            return NoContent();

        // Extract the access-token jti + expiry so the handler can blocklist it.
        var jti = User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Jti);
        var expClaim = User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Exp);
        DateTimeOffset? expiresAt = long.TryParse(expClaim, out var expSeconds)
            ? DateTimeOffset.FromUnixTimeSeconds(expSeconds)
            : null;

        await sender.Send(new LogoutCommand(userId, rawToken, jti, expiresAt), cancellationToken);

        if (TryGetSessionId(out var sessionId))
            await jtiBlocklist.AddAsync($"session:{sessionId:N}", DateTimeOffset.UtcNow.AddDays(90), cancellationToken);

        // Must match the Path used when the cookie was set.
        Response.Cookies.Delete("refreshToken", new CookieOptions { Path = "/api/auth" });
        return NoContent();
    }

    [HttpGet("me")]
    [Authorize]
    [EnableRateLimiting("public-api")] // 30/min — each call issues GetMyProfileQuery (DB SELECT)
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyProfile(CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        if (userIdClaim is null || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        var result = await sender.Send(new GetMyProfileQuery(userId), cancellationToken);

        if (result.IsFailure)
            return NotFound(new ProblemDetails { Title = "User not found", Status = 404 });

        return Ok(result.Value);
    }

    [HttpPatch("me")]
    [Authorize]
    [EnableRateLimiting("public-api")] // 30/min — each call issues UpdateUserProfileCommand (DB write)
    [RequestSizeLimit(4096)]           // Name ≤ 200 chars; 4 KB ceiling stops oversized JSON
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateMyProfile(
        [FromBody] UpdateMyProfileRequest request,
        CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        if (userIdClaim is null || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        var result = await sender.Send(
            new UpdateUserProfileCommand(userId, request.Name),
            cancellationToken);

        if (result.IsFailure)
            return BadRequest(new ProblemDetails
            {
                Title = "Profile update failed",
                Detail = string.Join("; ", result.Errors),
                Status = 400,
            });

        return NoContent();
    }
    [HttpPatch("me/password")]
    [Authorize]
    [EnableRateLimiting("change-password")]
    [RequestSizeLimit(1024)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ChangePassword(
        [FromBody] ChangePasswordRequest request,
        CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        if (userIdClaim is null || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        var result = await sender.Send(
            new ChangePasswordCommand(userId, request.CurrentPassword, request.NewPassword),
            cancellationToken);

        if (result.IsFailure)
            return BadRequest(new ProblemDetails
            {
                Title = "Password change failed",
                Detail = string.Join("; ", result.Errors),
                Status = 400,
            });

        return NoContent();
    }

    [HttpDelete("me")]
    [Authorize]
    [EnableRateLimiting("public-api")]
    [RequestSizeLimit(512)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DeleteAccount(
        [FromBody] DeleteAccountRequest request,
        CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        if (userIdClaim is null || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        var result = await sender.Send(
            new DeleteAccountCommand(userId, request.ConfirmPassword),
            cancellationToken);

        if (result.IsFailure)
            return BadRequest(new ProblemDetails
            {
                Title = "Account deletion failed",
                Detail = string.Join("; ", result.Errors),
                Status = 400,
            });

        Response.Cookies.Delete("refreshToken", new CookieOptions { Path = "/api/auth" });
        return NoContent();
    }

    [HttpPost("me/health-data-consent")]
    [Authorize]
    [EnableRateLimiting("public-api")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GrantHealthDataConsent(CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        if (userIdClaim is null || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        var result = await sender.Send(new GrantHealthDataConsentCommand(userId), cancellationToken);

        if (result.IsFailure)
            return BadRequest(new ProblemDetails { Title = "Consent could not be recorded", Detail = string.Join("; ", result.Errors), Status = 400 });

        return Ok(new { consentedAt = result.Value });
    }

    [HttpGet("me/export")]
    [Authorize]
    [EnableRateLimiting("data-export")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ExportMyData(CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        if (userIdClaim is null || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        var result = await sender.Send(new ExportMyDataQuery(userId), cancellationToken);

        if (result.IsFailure)
            return NotFound(new ProblemDetails { Title = "User not found", Status = 404 });

        return Ok(result.Value);
    }

    [HttpGet("me/sessions")]
    [Authorize]
    [EnableRateLimiting("public-api")]
    public async Task<IActionResult> GetMySessions(CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var currentSessionId = TryGetSessionId(out var sessionId) ? sessionId : (Guid?)null;
        var sessions = await refreshTokenRepository.GetActiveSessionsByUserIdAsync(userId, ct);
        return Ok(sessions.Select(session => new
        {
            session.SessionId,
            session.StartedAt,
            session.LastActivityAt,
            session.ExpiresAt,
            IsCurrent = currentSessionId == session.SessionId,
        }));
    }

    [HttpDelete("me/sessions/{sessionId:guid}")]
    [Authorize(Policy = "MfaStepUp")]
    [EnableRateLimiting("public-api")]
    public async Task<IActionResult> RevokeMySession(Guid sessionId, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        if (!await refreshTokenRepository.RevokeSessionAsync(userId, sessionId, ct)) return NotFound();
        await trustedDeviceRepository.RevokeForSessionAsync(userId, sessionId, ct);
        await unitOfWork.SaveChangesAsync(ct);
        await jtiBlocklist.AddAsync($"session:{sessionId:N}", DateTimeOffset.UtcNow.AddDays(90), ct);

        if (TryGetSessionId(out var currentSessionId) && currentSessionId == sessionId)
        {
            var jti = User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Jti);
            var expClaim = User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Exp);
            if (!string.IsNullOrWhiteSpace(jti) && long.TryParse(expClaim, out var expSeconds))
                await jtiBlocklist.AddAsync(jti, DateTimeOffset.FromUnixTimeSeconds(expSeconds), ct);
            Response.Cookies.Delete("refreshToken", new CookieOptions { Path = "/api/auth" });
        }

        return NoContent();
    }

    [HttpGet("me/trusted-devices")]
    [Authorize]
    [EnableRateLimiting("public-api")]
    public async Task<IActionResult> GetMyTrustedDevices(CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var devices = await trustedDeviceRepository.GetActiveByUserIdAsync(userId, ct);
        return Ok(devices.Select(device => new
        {
            device.Id,
            device.DeviceName,
            device.CreatedAt,
            device.LastUsedAt,
            device.ExpiresAt,
            device.LastSessionId,
        }));
    }

    [HttpPost("me/trusted-devices")]
    [Authorize(Policy = "MfaStepUp")]
    [EnableRateLimiting("public-api")]
    [RequestSizeLimit(512)]
    public async Task<IActionResult> TrustCurrentDevice([FromBody] TrustDeviceRequest request, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        if (!TryGetSessionId(out var sessionId))
            return BadRequest(new ProblemDetails { Detail = "La sesión actual no tiene identificador de sesión.", Status = 400 });
        var user = await userRepository.GetByIdAsync(userId, ct);
        if (user is null) return Unauthorized();
        if (!user.HasMfa)
            return UnprocessableEntity(new ProblemDetails { Detail = "Configure MFA antes de confiar este dispositivo.", Status = 422 });
        if (string.IsNullOrWhiteSpace(request.DeviceName) || request.DeviceName.Length > 100)
            return BadRequest(new ProblemDetails { Detail = "El nombre del dispositivo debe tener entre 1 y 100 caracteres.", Status = 400 });

        var (device, rawToken) = PawTrack.Domain.Auth.TrustedDevice.Create(
            userId, sessionId, request.DeviceName, TimeSpan.FromDays(30));
        await trustedDeviceRepository.AddAsync(device, ct);
        await unitOfWork.SaveChangesAsync(ct);
        Response.Cookies.Append("trustedDevice", rawToken, TrustedDeviceCookieOptions(device.ExpiresAt));
        return Created(string.Empty, new { device.Id, device.DeviceName, device.CreatedAt, device.ExpiresAt });
    }

    [HttpDelete("me/trusted-devices/{deviceId:guid}")]
    [Authorize(Policy = "MfaStepUp")]
    [EnableRateLimiting("public-api")]
    public async Task<IActionResult> RevokeTrustedDevice(Guid deviceId, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var device = await trustedDeviceRepository.GetByIdAsync(userId, deviceId, ct);
        if (device is null || !device.IsActive) return NotFound();
        device.Revoke();
        trustedDeviceRepository.Update(device);
        await unitOfWork.SaveChangesAsync(ct);

        if (Request.Cookies.TryGetValue("trustedDevice", out var rawToken)
            && device.TokenHash == ComputeHash(rawToken))
            Response.Cookies.Delete("trustedDevice", new CookieOptions { Path = "/api/auth" });
        return NoContent();
    }

    private bool TryGetUserId(out Guid userId)
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(raw, out userId);
    }

    private bool TryGetSessionId(out Guid sessionId) => Guid.TryParse(User.FindFirstValue("sid"), out sessionId);

    private CookieOptions TrustedDeviceCookieOptions(DateTimeOffset? expiresAt = null) => new()
    {
        HttpOnly = true,
        Secure = !environment.IsDevelopment(),
        SameSite = SameSiteMode.Strict,
        Expires = expiresAt ?? DateTimeOffset.UtcNow.AddDays(30),
        Path = "/api/auth",
    };

    private static string ComputeHash(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token))).ToLowerInvariant();
}

public sealed record EnableMfaRequest(string Secret, string Code);
public sealed record MfaStepUpRequest(string Code);

// Request models — co-located with controller
public sealed record RegisterRequest(string Name, string Email, string Password, bool IsAdultConfirmed);
public sealed record LoginRequest(string Email, string Password, string? MfaCode = null);
public sealed record TrustDeviceRequest(string DeviceName);
public sealed record ForgotPasswordRequest(string Email);
public sealed record ResetPasswordRequest(string Token, string NewPassword);
public sealed record UpdateMyProfileRequest(string Name);
public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);
public sealed record DeleteAccountRequest(string ConfirmPassword);
