namespace PawTrack.API.Middleware;

/// <summary>
/// Injects enterprise-grade HTTP security headers on every response.
/// Covers OWASP Top 10 A05 (Security Misconfiguration) mitigations.
/// </summary>
public sealed class SecurityHeadersMiddleware(RequestDelegate next, IWebHostEnvironment env)
{
    // CSP for the API — permits app insights telemetry and blob storage images.
    // Frontend assets are served by the SWA CDN, not this API, so 'self' is tight.
    private const string CspValue =
        "default-src 'none'; " +
        "frame-ancestors 'none'";

    public async Task InvokeAsync(HttpContext context)
    {
        var headers = context.Response.Headers;

        // ── Clickjacking ──────────────────────────────────────────────────────
        headers["X-Frame-Options"] = "DENY";

        // ── MIME sniffing ─────────────────────────────────────────────────────
        headers["X-Content-Type-Options"] = "nosniff";
        // ── Legacy XSS auditor — disable the buggy browser filter; rely on CSP ──
        // Setting to "0" explicitly disables Internet Explorer / early Chrome XSS
        // auditor which has documented bypass bugs and can itself be exploited.
        // Per OWASP, modern apps should disable it and rely on CSP instead.
        headers["X-XSS-Protection"] = "0";
        // ── HSTS (only meaningful over HTTPS; skip in dev to avoid breaking localhost) ──
        if (!env.IsDevelopment())
        {
            headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains; preload";
        }

        // ── Referrer ──────────────────────────────────────────────────────────
        headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

        // ── Content Security Policy ───────────────────────────────────────────
        headers["Content-Security-Policy"] = CspValue;

        // ── Permissions Policy ────────────────────────────────────────────────
        // Geolocation and camera are used by the PWA frontend (served by SWA, not
        // this API), so we explicitly deny them at the API layer.
        headers["Permissions-Policy"] = "geolocation=(), camera=(), microphone=(), payment=(), usb=()";

        // ── Cross-origin isolation (R79-R80) ──────────────────────────────────
        headers["Cross-Origin-Resource-Policy"] = "same-origin";
        headers["Cross-Origin-Opener-Policy"] = "same-origin";

        // ── Remove fingerprinting headers added by Kestrel / IIS ─────────────
        headers.Remove("Server");
        headers.Remove("X-Powered-By");
        headers.Remove("X-AspNet-Version");
        headers.Remove("X-AspNetMvc-Version");

        await next(context);
    }
}
