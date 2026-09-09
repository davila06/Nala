using System.Text;
using System.Text.Json;
using Fido2NetLib;
using Fido2NetLib.Objects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using PawTrack.Application.Auth.DTOs;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Auth;

namespace PawTrack.API.Controllers;

[ApiController]
[Route("api/auth/webauthn")]
public sealed class WebAuthnController(
    Fido2 fido2,
    IUserRepository userRepository,
    IWebAuthnCredentialRepository credentialRepository,
    IDistributedCache cache,
    IUnitOfWork unitOfWork,
    IJwtTokenService jwtTokenService,
    IHostEnvironment environment) : ControllerBase
{
    private static readonly TimeSpan ChallengeLifetime = TimeSpan.FromMinutes(5);

    [HttpPost("register/options")]
    [Authorize]
    public async Task<IActionResult> RegisterOptions(CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var user = await userRepository.GetByIdAsync(userId, ct);
        if (user is null) return Unauthorized();
        var credentials = await credentialRepository.GetByUserIdAsync(userId, ct);
        var options = fido2.RequestNewCredential(new RequestNewCredentialParams
        {
            User = new Fido2User
            {
                Id = user.Id.ToByteArray(),
                Name = user.Email,
                DisplayName = user.Name,
            },
            ExcludeCredentials = credentials
                .Select(x => new PublicKeyCredentialDescriptor(x.CredentialId))
                .ToList(),
            AuthenticatorSelection = AuthenticatorSelection.Default,
            AttestationPreference = AttestationConveyancePreference.None,
            PubKeyCredParams = [
                new PubKeyCredParam(COSE.Algorithm.ES256, PublicKeyCredentialType.PublicKey),
                new PubKeyCredParam(COSE.Algorithm.RS256, PublicKeyCredentialType.PublicKey),
            ],
        });
        await cache.SetStringAsync(RegisterKey(userId), options.ToJson(), CacheOptions(), ct);
        return Content(options.ToJson(), "application/json");
    }

    [HttpPost("register")]
    [Authorize]
    public async Task<IActionResult> Register([FromBody] WebAuthnRegisterRequest request, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var json = await cache.GetStringAsync(RegisterKey(userId), ct);
        if (string.IsNullOrWhiteSpace(json)) return BadRequest(new ProblemDetails { Detail = "El desafío WebAuthn expiró." });
        await cache.RemoveAsync(RegisterKey(userId), ct);
        var options = CredentialCreateOptions.FromJson(json);
        var response = JsonSerializer.Deserialize<AuthenticatorAttestationRawResponse>(request.Response.GetRawText());
        if (response is null) return BadRequest(new ProblemDetails { Detail = "Respuesta WebAuthn inválida." });

        var result = await fido2.MakeNewCredentialAsync(new MakeNewCredentialParams
        {
            AttestationResponse = response,
            OriginalOptions = options,
            IsCredentialIdUniqueToUserCallback = async (args, callbackCt) =>
                await credentialRepository.GetByCredentialIdAsync(args.CredentialId, callbackCt) is null,
        }, ct);

        var credential = WebAuthnCredential.Create(
            userId, result.Id, result.PublicKey, result.SignCount, request.DeviceName);
        await credentialRepository.AddAsync(credential, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return Ok(new { credentialId = Convert.ToBase64String(result.Id) });
    }

    [HttpPost("authenticate/options")]
    [AllowAnonymous]
    public async Task<IActionResult> AuthenticateOptions([FromBody] WebAuthnOptionsRequest request, CancellationToken ct)
    {
        var user = await userRepository.GetByEmailAsync(request.Email, ct);
        if (user is null) return Unauthorized(new ProblemDetails { Detail = "Autenticación no válida." });
        var credentials = await credentialRepository.GetByUserIdAsync(user.Id, ct);
        if (credentials.Count == 0) return Unauthorized(new ProblemDetails { Detail = "La cuenta no tiene passkeys." });
        var options = fido2.GetAssertionOptions(new GetAssertionOptionsParams
        {
            AllowedCredentials = credentials
                .Select(x => new PublicKeyCredentialDescriptor(x.CredentialId))
                .ToList(),
            UserVerification = UserVerificationRequirement.Preferred,
        });
        await cache.SetStringAsync(AuthenticateKey(user.Id), options.ToJson(), CacheOptions(), ct);
        return Content(options.ToJson(), "application/json");
    }

    [HttpPost("authenticate")]
    [AllowAnonymous]
    public async Task<IActionResult> Authenticate([FromBody] WebAuthnAuthenticateRequest request, CancellationToken ct)
    {
        var user = await userRepository.GetByEmailAsync(request.Email, ct);
        if (user is null) return Unauthorized(new ProblemDetails { Detail = "Autenticación no válida." });
        var json = await cache.GetStringAsync(AuthenticateKey(user.Id), ct);
        if (string.IsNullOrWhiteSpace(json)) return Unauthorized(new ProblemDetails { Detail = "El desafío WebAuthn expiró." });
        await cache.RemoveAsync(AuthenticateKey(user.Id), ct);
        var options = AssertionOptions.FromJson(json);
        var response = JsonSerializer.Deserialize<AuthenticatorAssertionRawResponse>(request.Response.GetRawText());
        if (response is null) return Unauthorized(new ProblemDetails { Detail = "Respuesta WebAuthn inválida." });
        var credential = await credentialRepository.GetByCredentialIdAsync(response.RawId, ct);
        if (credential is null || credential.UserId != user.Id) return Unauthorized();

        var result = await fido2.MakeAssertionAsync(new MakeAssertionParams
        {
            AssertionResponse = response,
            OriginalOptions = options,
            StoredPublicKey = credential.PublicKey,
            StoredSignatureCounter = credential.SignatureCounter,
            IsUserHandleOwnerOfCredentialIdCallback = (args, _) => Task.FromResult(
                args.UserHandle.SequenceEqual(user.Id.ToByteArray())
                && args.CredentialId.SequenceEqual(credential.CredentialId)),
        }, ct);
        if (!credential.UpdateCounter(result.SignCount)) return Unauthorized();
        credentialRepository.Update(credential);
        await unitOfWork.SaveChangesAsync(ct);
        var (rawRefresh, refreshHash) = jwtTokenService.GenerateRefreshToken();
        user.AddRefreshToken(refreshHash, DateTimeOffset.UtcNow.AddDays(30));
        userRepository.Update(user);
        await unitOfWork.SaveChangesAsync(ct);
        Response.Cookies.Append("refreshToken", rawRefresh, new CookieOptions
        {
            HttpOnly = true,
            Secure = !environment.IsDevelopment(),
            SameSite = SameSiteMode.Lax,
            Expires = DateTimeOffset.UtcNow.AddDays(30),
            Path = "/api/auth",
        });
        return Ok(new
        {
            accessToken = jwtTokenService.GenerateAccessToken(user.Id, user.Email, user.Name, user.Role),
            user = UserProfileDto.FromDomain(user),
        });
    }

    [HttpDelete("{credentialId}")]
    [Authorize]
    public async Task<IActionResult> Revoke(string credentialId, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        if (!Convert.TryFromBase64String(credentialId, new byte[1024], out var length)) return NotFound();
        var bytes = Convert.FromBase64String(credentialId);
        var credential = await credentialRepository.GetByCredentialIdAsync(bytes, ct);
        if (credential is null || credential.UserId != userId) return NotFound();
        credential.Revoke();
        credentialRepository.Update(credential);
        await unitOfWork.SaveChangesAsync(ct);
        return NoContent();
    }

    private static DistributedCacheEntryOptions CacheOptions() => new() { AbsoluteExpirationRelativeToNow = ChallengeLifetime };
    private static string RegisterKey(Guid userId) => $"webauthn:register:{userId:N}";
    private static string AuthenticateKey(Guid userId) => $"webauthn:authenticate:{userId:N}";

    private bool TryGetUserId(out Guid userId) =>
        Guid.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out userId);
}

public sealed record WebAuthnRegisterRequest(JsonElement Response, string? DeviceName);
public sealed record WebAuthnOptionsRequest(string Email);
public sealed record WebAuthnAuthenticateRequest(string Email, JsonElement Response);