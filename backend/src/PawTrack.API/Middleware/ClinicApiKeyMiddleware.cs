using PawTrack.Application.Clinics;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Clinics;
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

    public async Task InvokeAsync(
        HttpContext context,
        IClinicApiKeyRepository keyRepository,
        IClinicRepository clinicRepository,
        IUnitOfWork unitOfWork)
    {
        if (context.Request.Headers.TryGetValue(HeaderName, out var rawKeyValues)
            && !string.IsNullOrWhiteSpace((string?)rawKeyValues))
        {
            var rawKey = (string)rawKeyValues!;
            var hash = ClinicApiKeyHasher.Compute(rawKey);
            var key = await keyRepository.GetByHashAsync(hash, context.RequestAborted);

            if (key is not null)
            {
                var clinic = await clinicRepository.GetByIdAsync(key.ClinicId, context.RequestAborted);
                if (clinic is null || clinic.Status != ClinicStatus.Active)
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    await context.Response.WriteAsJsonAsync(new { title = "Clinic API key is not active.", status = 401 });
                    return;
                }

                var identity = new ClaimsIdentity("ApiKey");
                identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, clinic.UserId.ToString()));
                identity.AddClaim(new Claim(ClaimTypes.Role, "Clinic"));
                identity.AddClaim(new Claim("ClinicId", key.ClinicId.ToString()));
                identity.AddClaim(new Claim("ClinicApiKeyId", key.Id.ToString()));
                foreach (var scope in key.GetScopes())
                    identity.AddClaim(new Claim("ClinicApiScope", scope));
                context.User = new ClaimsPrincipal(identity);

                if (IsHumanOnlyManagementPath(context.Request.Method, context.Request.Path))
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    await context.Response.WriteAsJsonAsync(new
                    {
                        title = "Clinic API keys cannot access human management endpoints.",
                        status = 403,
                    });
                    return;
                }

                var requiredScope = RequiredScope(context.Request.Method, context.Request.Path);
                if (requiredScope is not null && !key.HasScope(requiredScope))
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    await context.Response.WriteAsJsonAsync(new
                    {
                        title = "API key scope is insufficient.",
                        status = 403,
                        requiredScope,
                    });
                    return;
                }

                try
                {
                    key.RecordUsage();
                    keyRepository.Update(key);
                    await unitOfWork.SaveChangesAsync(context.RequestAborted);
                }
                catch (Exception ex)
                {
                    // Telemetry must not turn a valid partner request into a 500.
                    logger.LogWarning(ex, "Failed to record API key usage for key {KeyId}", key.Id);
                }
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

    private static string? RequiredScope(string method, PathString path)
    {
        if (path.StartsWithSegments("/api/clinics/scan") && HttpMethods.IsPost(method))
            return ClinicApiScope.Scan;
        if (path.StartsWithSegments("/api/v1/pets/lookup"))
            return ClinicApiScope.Scan;
        if (path.StartsWithSegments("/api/clinics/patients/medical")
            || path.StartsWithSegments("/api/clinics/patients"))
        {
            if (path.Value?.EndsWith("/export", StringComparison.OrdinalIgnoreCase) == true)
                return ClinicApiScope.MedicalExport;
            return HttpMethods.IsGet(method) ? ClinicApiScope.MedicalRead : ClinicApiScope.MedicalWrite;
        }
        if (path.StartsWithSegments("/api/clinics/medical-exports"))
            return ClinicApiScope.MedicalExport;
        if (path.StartsWithSegments("/api/certificates"))
            return ClinicApiScope.Certificates;
        if (path.StartsWithSegments("/api/clinics/me/stats") || path.StartsWithSegments("/api/clinics/me/visibility-stats"))
            return ClinicApiScope.Analytics;
        return null;
    }

    private static bool IsHumanOnlyManagementPath(string method, PathString path) =>
        path.StartsWithSegments("/api/clinics/me/api-keys")
        || path.StartsWithSegments("/api/clinics/profile")
        || path.StartsWithSegments("/api/clinics/me/profile")
        || path.StartsWithSegments("/api/clinics/me/veterinarians")
        || path.StartsWithSegments("/api/clinics/me/appointments")
        || (path.StartsWithSegments("/api/clinics/me") && !path.StartsWithSegments("/api/clinics/me/stats")
            && !path.StartsWithSegments("/api/clinics/me/visibility-stats"));
}
