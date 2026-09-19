using System.Globalization;

namespace PawTrack.API.Middleware;

public sealed class LegacyPartnerApiDeprecationMiddleware(
    RequestDelegate next,
    IConfiguration configuration)
{
    private static readonly string[] LegacyPrefixes =
    [
        "/api/clinics",
        "/api/certificates",
        "/api/widget",
        "/api/webhooks",
        "/api/product-events",
    ];

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? string.Empty;
        var isLegacyPartnerRoute = LegacyPrefixes.Any(prefix =>
            path.Equals(prefix, StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith(prefix + "/", StringComparison.OrdinalIgnoreCase));

        if (isLegacyPartnerRoute)
        {
            var successor = path.Replace("/api/", "/api/v1/", StringComparison.OrdinalIgnoreCase);
            var sunset = configuration.GetValue<DateTimeOffset?>("ApiVersioning:LegacyPartnerSunsetUtc")
                ?? new DateTimeOffset(2027, 3, 18, 0, 0, 0, TimeSpan.Zero);

            context.Response.OnStarting(() =>
            {
                context.Response.Headers["Deprecation"] = "true";
                context.Response.Headers["Sunset"] = sunset.ToString("R", CultureInfo.InvariantCulture);
                context.Response.Headers.Link = $"<{successor}>; rel=\"successor-version\"";
                return Task.CompletedTask;
            });
        }

        await next(context);
    }
}
