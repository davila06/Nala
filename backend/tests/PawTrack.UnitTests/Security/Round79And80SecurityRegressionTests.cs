using FluentAssertions;
using Microsoft.AspNetCore.Http;
using PawTrack.API.Middleware;

namespace PawTrack.UnitTests.Security;

/// <summary>
/// Round-79 and Round-80 security regression tests.
///
/// Gaps: <c>SecurityHeadersMiddleware</c> already sets X-Frame-Options, HSTS,
/// X-Content-Type-Options, Referrer-Policy, CSP, and Permissions-Policy,
/// but is missing two cross-origin isolation headers introduced as OWASP
/// hardening recommendations:
///
///   R79 — <c>Cross-Origin-Resource-Policy: same-origin</c>
///     Prevents cross-origin sites from embedding API responses (images, JSON)
///     as sub-resources.  Without it, a malicious third-party page can request
///     the API's signed blob-storage URLs via <c>&lt;img&gt;</c> or <c>&lt;script&gt;</c>
///     tags, potentially leaking response metadata via Spectre side-channels or
///     exploiting CSRF-adjacent attacks.
///
///   R80 — <c>Cross-Origin-Opener-Policy: same-origin</c>
///     Opts the API's browsing context into process isolation (COEP complement).
///     Prevents an attacker from obtaining a window reference to the API via
///     <c>window.open()</c> and using shared-memory/timing attacks to read
///     response data.  It is also required to enable SharedArrayBuffer in
///     browsers — preventing third parties from abusing that API against clients
///     that touch the API's pages.
///
/// Fix:
///   Add the two headers to the <c>InvokeAsync</c> response-header block in
///   <c>SecurityHeadersMiddleware.cs</c>.
/// </summary>
public sealed class Round79And80SecurityRegressionTests
{
    // ── Test helper ───────────────────────────────────────────────────────────

    /// <summary>
    /// Runs the <c>SecurityHeadersMiddleware</c> against a minimal HttpContext
    /// and returns the response headers written into it.
    /// Does NOT require a running web server.
    /// </summary>
    private static async Task<IHeaderDictionary> InvokeMiddlewareAsync()
    {
        var context = new DefaultHttpContext();

        // Stub environment for the middleware (non-development → HSTS is set)
        var env = new StubWebHostEnvironment();

        var middleware = new SecurityHeadersMiddleware(
            next: _ => Task.CompletedTask,
            env: env);

        await middleware.InvokeAsync(context);

        return context.Response.Headers;
    }

    // ── R79 — Cross-Origin-Resource-Policy ────────────────────────────────────

    [Fact]
    public async Task SecurityHeaders_Include_CrossOriginResourcePolicy()
    {
        var headers = await InvokeMiddlewareAsync();

        headers.Should().ContainKey("Cross-Origin-Resource-Policy",
            "CORP: same-origin prevents third-party sites from loading API responses " +
            "as sub-resources, closing a Spectre side-channel and CSRF-adjacent leak");

        headers["Cross-Origin-Resource-Policy"].ToString()
            .Should().Be("same-origin",
                "the strongest CORP value appropriate for a same-origin JSON API");
    }

    // ── R80 — Cross-Origin-Opener-Policy ──────────────────────────────────────

    [Fact]
    public async Task SecurityHeaders_Include_CrossOriginOpenerPolicy()
    {
        var headers = await InvokeMiddlewareAsync();

        headers.Should().ContainKey("Cross-Origin-Opener-Policy",
            "COOP: same-origin isolates the API's browsing context from opener " +
            "window references, hardening against Spectre memory-timing attacks");

        headers["Cross-Origin-Opener-Policy"].ToString()
            .Should().Be("same-origin",
                "the strongest COOP value; safe for a JSON API that is never " +
                "cross-origin opened by legitimate callers");
    }
}

// ── Stubs ─────────────────────────────────────────────────────────────────────

/// <summary>
/// Minimal <c>IWebHostEnvironment</c> stub that reports non-development so the
/// middleware writes the HSTS header on top of the two new headers being tested.
/// </summary>
file sealed class StubWebHostEnvironment : Microsoft.AspNetCore.Hosting.IWebHostEnvironment
{
    public string EnvironmentName { get; set; } = "Production";
    public string ApplicationName { get; set; } = "PawTrack.API";
    public string WebRootPath { get; set; } = string.Empty;
    public Microsoft.Extensions.FileProviders.IFileProvider WebRootFileProvider { get; set; } =
        new Microsoft.Extensions.FileProviders.NullFileProvider();
    public string ContentRootPath { get; set; } = string.Empty;
    public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } =
        new Microsoft.Extensions.FileProviders.NullFileProvider();
}
