using PawTrack.Application.Collars;
using PawTrack.Application.Common.Interfaces;
using System.Security.Claims;

namespace PawTrack.API.Middleware;

/// <summary>
/// Authenticates device-to-server ingest requests via the X-Collar-Key header.
/// Runs before UseAuthentication so the CollarId claim is available to controllers.
/// </summary>
public sealed class CollarDeviceKeyMiddleware(
    RequestDelegate next,
    ILogger<CollarDeviceKeyMiddleware> logger)
{
    private const string HeaderName = "X-Collar-Key";

    public async Task InvokeAsync(
        HttpContext context,
        ICollarDeviceCredentialRepository credentialRepository,
        IUnitOfWork unitOfWork)
    {
        if (context.Request.Headers.TryGetValue(HeaderName, out var rawKeyValues)
            && !string.IsNullOrWhiteSpace((string?)rawKeyValues))
        {
            var rawKey = (string)rawKeyValues!;
            var hash = CollarDeviceKeyHasher.Compute(rawKey);
            var credential = await credentialRepository.GetActiveByHashAsync(hash, context.RequestAborted);

            if (credential is not null)
            {
                var identity = new ClaimsIdentity("CollarDeviceKey");
                identity.AddClaim(new Claim("CollarId", credential.CollarId.ToString()));
                context.User = new ClaimsPrincipal(identity);

                _ = Task.Run(async () =>
                {
                    try
                    {
                        credential.RecordUsage();
                        credentialRepository.Update(credential);
                        await unitOfWork.SaveChangesAsync(default);
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "Failed to record collar key usage for credential {CredentialId}", credential.Id);
                    }
                });
            }
            else
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(new { title = "Invalid or revoked device key.", status = 401 });
                return;
            }
        }

        await next(context);
    }
}
