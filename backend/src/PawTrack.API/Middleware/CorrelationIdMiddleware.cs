namespace PawTrack.API.Middleware;

/// <summary>
/// Propagates a per-request correlation ID for distributed tracing.
/// <para>
/// Security: the client-supplied <c>X-Correlation-Id</c> header is accepted
/// ONLY when it is a valid UUID (v4 or v7). Any other value — including
/// oversized strings crafted for log-injection or log-flooding — is silently
/// replaced with a server-generated UUID v7.
/// This prevents OWASP A09 log injection and Application Insights flooding.
/// </para>
/// </summary>
public sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    private const string HeaderName = "X-Correlation-Id";

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = ResolveCorrelationId(context);

        context.Items[HeaderName] = correlationId;
        context.Response.Headers[HeaderName] = correlationId;

        await next(context);
    }

    private static string ResolveCorrelationId(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(HeaderName, out var raw)
            && !string.IsNullOrWhiteSpace(raw)
            && Guid.TryParse(raw.ToString(), out _))
        {
            // Client-supplied value is a valid GUID — safe to propagate.
            return raw.ToString();
        }

        // Generate a fresh v7 correlation ID (time-sortable, opaque to clients).
        return Guid.CreateVersion7().ToString();
    }
}
