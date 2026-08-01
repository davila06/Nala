using PawTrack.Application.Clinics;
using PawTrack.Application.Common.Interfaces;
using System.Security.Claims;

namespace PawTrack.API.Middleware;

/// <summary>
/// Handles X-PawTrack-Key header for machine-to-machine API access (ClinicPartner tier).
/// Runs before UseAuthentication so that clinic API key requests arrive authenticated.
/// </summary>
public sealed class ClinicApiKeyMiddleware(
    RequestDelegate next,
    ILogger<ClinicApiKeyMiddleware> logger)
{
    private const string HeaderName = "X-PawTrack-Key";

    public async Task InvokeAsync(HttpContext context, IClinicApiKeyRepository keyRepository, IUnitOfWork unitOfWork)
    {
        if (context.Request.Headers.TryGetValue(HeaderName, out var rawKeyValues)
            && !string.IsNullOrWhiteSpace((string?)rawKeyValues))
        {
            var rawKey = (string)rawKeyValues!;
            var hash = ClinicApiKeyHasher.Compute(rawKey);
            var key = await keyRepository.GetByHashAsync(hash, context.RequestAborted);

            if (key is not null)
            {
                var identity = new ClaimsIdentity("ApiKey");
                identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, key.ClinicId.ToString()));
                identity.AddClaim(new Claim(ClaimTypes.Role, "Clinic"));
                identity.AddClaim(new Claim("ClinicApiKeyId", key.Id.ToString()));
                context.User = new ClaimsPrincipal(identity);

                _ = Task.Run(async () =>
                {
                    try
                    {
                        key.RecordUsage();
                        keyRepository.Update(key);
                        await unitOfWork.SaveChangesAsync(default);
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "Failed to record API key usage for key {KeyId}", key.Id);
                    }
                });
            }
            else
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(new { title = "Invalid or revoked API key.", status = 401 });
                return;
            }
        }

        await next(context);
    }
}
