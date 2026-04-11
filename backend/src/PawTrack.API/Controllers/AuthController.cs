using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PawTrack.Application.Auth.Commands.ForgotPassword;
using PawTrack.Application.Auth.Commands.Login;
using PawTrack.Application.Auth.Commands.Logout;
using PawTrack.Application.Auth.Commands.RefreshToken;
using PawTrack.Application.Auth.Commands.Register;
using PawTrack.Application.Auth.Commands.ResetPassword;
using PawTrack.Application.Auth.Commands.UpdateUserProfile;
using PawTrack.Application.Auth.Commands.VerifyEmail;
using PawTrack.Application.Auth.Queries.GetMyProfile;
using System.Security.Claims;

namespace PawTrack.API.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(ISender sender) : ControllerBase
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
            new RegisterCommand(request.Name, request.Email, request.Password),
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
        var result = await sender.Send(new LoginCommand(request.Email, request.Password), cancellationToken);

        if (result.IsFailure)
            return Unauthorized(new ProblemDetails { Title = "Authentication failed", Detail = string.Join("; ", result.Errors), Status = 401 });

        var token = result.Value!;

        // Refresh token in HttpOnly cookie.
        // SameSite=Lax: safer than None, compatible with OAuth redirect flows
        // that land back on this origin. Strict would drop the cookie on cross-site
        // redirects (e.g., social login callbacks).
        Response.Cookies.Append("refreshToken", token.RefreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
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
            Secure = true,
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
        var jti         = User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Jti);
        var expClaim    = User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Exp);
        DateTimeOffset? expiresAt = long.TryParse(expClaim, out var expSeconds)
            ? DateTimeOffset.FromUnixTimeSeconds(expSeconds)
            : null;

        await sender.Send(new LogoutCommand(userId, rawToken, jti, expiresAt), cancellationToken);

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
}

// Request models — co-located with controller
public sealed record RegisterRequest(string Name, string Email, string Password);
public sealed record LoginRequest(string Email, string Password);
public sealed record ForgotPasswordRequest(string Email);
public sealed record ResetPasswordRequest(string Token, string NewPassword);
public sealed record UpdateMyProfileRequest(string Name);
