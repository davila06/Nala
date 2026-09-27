using System.Security.Claims;
using PawTrack.Application.Common.Interfaces;

namespace PawTrack.API.Middleware;

public sealed class ActiveClinicSiteMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(
        HttpContext httpContext,
        IActiveClinicSiteContext siteContext,
        IClinicSiteAccessRepository siteAccessRepository)
    {
        var principal = httpContext.User;
        var userId = Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var parsedUserId)
            ? parsedUserId
            : (Guid?)null;
        var sessionId = Guid.TryParse(principal.FindFirstValue("sid"), out var parsedSessionId)
            ? parsedSessionId
            : (Guid?)null;
        var isApiKey = principal.Identities.Any(identity => identity.IsAuthenticated
            && string.Equals(identity.AuthenticationType, "ApiKey", StringComparison.Ordinal));
        var isClinicRoute = IsClinicRoute(httpContext.Request.Path);
        var isClinicPrincipal = isApiKey || (principal.Identity?.IsAuthenticated == true && isClinicRoute);
        var isAdministrator = principal.IsInRole("Admin") || principal.IsInRole("SuperAdmin");
        Guid? clinicId = null;

        if (isClinicPrincipal && userId.HasValue)
        {
            if (isApiKey && Guid.TryParse(principal.FindFirstValue("ClinicId"), out var apiKeyClinicId))
            {
                if (await siteAccessRepository.HasAccessAsync(userId.Value, apiKeyClinicId, httpContext.RequestAborted))
                    clinicId = apiKeyClinicId;
            }
            else if (sessionId.HasValue)
            {
                clinicId = await siteAccessRepository.GetActiveClinicIdAsync(
                    userId.Value, sessionId.Value, httpContext.RequestAborted);

                if (!clinicId.HasValue)
                {
                    var accessibleSites = await siteAccessRepository.ListAccessibleSitesAsync(userId.Value, httpContext.RequestAborted);
                    if (accessibleSites.Count == 1
                        && await siteAccessRepository.HasAccessAsync(userId.Value, accessibleSites[0].ClinicId, httpContext.RequestAborted))
                        clinicId = accessibleSites[0].ClinicId;
                }
            }
        }

        siteContext.Initialize(userId, sessionId, clinicId, isClinicPrincipal, isAdministrator, isApiKey);
        await next(httpContext);
    }

    private static bool IsClinicRoute(PathString path) =>
        path.StartsWithSegments("/api/clinics")
        || path.StartsWithSegments("/api/v1/clinics")
        || path.StartsWithSegments("/api/certificates")
        || path.StartsWithSegments("/api/v1/certificates")
        || path.StartsWithSegments("/api/medical")
        || path.StartsWithSegments("/api/v1/medical")
        || path.StartsWithSegments("/api/pet-clinic-access")
        || path.StartsWithSegments("/api/v1/pet-clinic-access")
        || path.StartsWithSegments("/api/castration-campaigns")
        || path.StartsWithSegments("/api/v1/castration-campaigns");
}
