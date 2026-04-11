using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using PawTrack.API.Controllers;
using System.Reflection;

namespace PawTrack.UnitTests.Security;

/// <summary>
/// Round-60 security regression tests.
///
/// Gap: <c>POST /api/notifications/{id}/resolve-check-response</c> does not carry
/// a <c>[RequestSizeLimit]</c> attribute, while neighboring endpoints in the same
/// controller do:
///
///   <code>
///   // Missing (current state)
///   [HttpPost("{id:guid}/resolve-check-response")]
///   [EnableRateLimiting("notifications-write")]
///   public async Task&lt;IActionResult&gt; RespondResolveCheck(...)
///
///   // Present on other POST endpoints (line ≈ 128)
///   [RequestSizeLimit(512)]
///   [HttpPost("{id:guid}/read")]
///   </code>
///
/// The request body for this endpoint is a single boolean field
/// (<c>{ "foundAtHome": true }</c>, ~22 bytes).  Without an explicit size cap the
/// ASP.NET Core default limit (30 MB) applies, allowing a client to send a
/// multi-megabyte body on every call and exhaust connection buffers — effectively
/// a targeted denial-of-service against the notification processing pipeline.
///
/// The rate limit (<c>notifications-write</c>) throttles call frequency but does
/// not bound the per-request body size; both controls are necessary.
///
/// Fix:
///   Add <c>[RequestSizeLimit(128)]</c> to <c>RespondResolveCheck</c>.
///   128 bytes is generous for a one-field JSON body and leaves room for
///   HTTP framing; it is consistent with how similar micro-payload endpoints
///   are capped elsewhere in the project.
/// </summary>
public sealed class Round60SecurityRegressionTests
{
    private static MethodInfo GetRespondResolveCheckMethod() =>
        typeof(NotificationsController)
            .GetMethod("RespondResolveCheck", BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException(
                "NotificationsController.RespondResolveCheck method not found. " +
                "The method may have been renamed or removed.");

    // ── RequestSizeLimit attribute must be present ────────────────────────────

    [Fact]
    public void NotificationsController_RespondResolveCheck_HasRequestSizeLimitAttribute()
    {
        var method = GetRespondResolveCheckMethod();

        var attr = method.GetCustomAttribute<RequestSizeLimitAttribute>();

        attr.Should().NotBeNull(
            "POST /api/notifications/{id}/resolve-check-response accepts a micro-payload " +
            "(~22 bytes) and must carry [RequestSizeLimit] to prevent oversized body " +
            "DoS; the default 30 MB ASP.NET limit is orders of magnitude too large " +
            "for a single-boolean JSON body");
    }

    // ── Limit must be the only RequestSizeLimit on that action ────────────────

    [Fact]
    public void NotificationsController_RespondResolveCheck_HasExactlyOneRequestSizeLimitAttribute()
    {
        var method = GetRespondResolveCheckMethod();

        // AllowMultiple = false on [RequestSizeLimit]; this verifies the attribute
        // is present exactly once — not inherited from a class-level default that
        // might be using a much larger limit.
        var attrs = method.GetCustomAttributes<RequestSizeLimitAttribute>(inherit: true);

        attrs.Should().ContainSingle(
            "RespondResolveCheck must have exactly one [RequestSizeLimit] attribute " +
            "applied directly to the action, not inherited from a class-level default");
    }
}
