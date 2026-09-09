using Microsoft.AspNetCore.Http;

namespace PawTrack.API;

/// <summary>
/// Extracts a per-client partition key from an <see cref="HttpContext"/> for use with
/// <see cref="System.Threading.RateLimiting.RateLimitPartition"/>.
///
/// Using the client's remote IP address as the partition key means each client IP gets
/// its own independent rate-limit quota window.  One client exhausting its quota does
/// not affect any other client's quota.
///
/// Falls back to <c>"anonymous"</c> when the remote IP cannot be resolved (e.g., in
/// unit-test environments using Unix sockets or when the connection info is unavailable).
/// All anonymous connections share the same "anonymous" bucket as a conservative fallback.
/// </summary>
public static class RateLimiterIpKey
{
    /// <summary>
    /// Returns the partition key for the given <paramref name="ctx"/>.
    /// </summary>
    public static string Get(HttpContext ctx) =>
        ctx.Connection.RemoteIpAddress?.ToString() ?? "anonymous";

    /// <summary>
    /// Uses the machine credential or authenticated subject when available so one
    /// tenant cannot consume another tenant's quota. Anonymous callers remain IP-based.
    /// </summary>
    public static string GetClient(HttpContext ctx)
    {
        var apiKeyId = ctx.User.FindFirst("ClinicApiKeyId")?.Value;
        if (!string.IsNullOrWhiteSpace(apiKeyId)) return $"api-key:{apiKeyId}";
        var subject = ctx.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? ctx.User.FindFirst("sub")?.Value;
        if (!string.IsNullOrWhiteSpace(subject)) return $"user:{subject}";
        return Get(ctx);
    }
}
