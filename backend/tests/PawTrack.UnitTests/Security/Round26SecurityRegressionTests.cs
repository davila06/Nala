using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PawTrack.API.Controllers;
using System.Reflection;

namespace PawTrack.UnitTests.Security;

/// <summary>
/// Round-26 security regression tests.
///
/// Gap: <c>PUT /api/allies/me/alerts/{notificationId:guid}/action</c>
/// (<c>AlliesController.ConfirmAction</c>) has <b>zero <c>[EnableRateLimiting]</c></b>
/// despite calling <c>unitOfWork.SaveChangesAsync()</c> on every invocation.
///
/// Attack vector:
///   The <c>ConfirmAllyAlertActionCommandHandler</c> performs the following DB
///   operations on <b>each call</b>:
///     ① <c>allyProfileRepository.GetVerifiedByUserIdAsync</c> — SELECT ally profile
///     ② <c>notificationRepository.GetByIdAsync</c>             — SELECT notification
///     ③ <c>notification.ConfirmAction(actionSummary)</c>        — mutates state
///     ④ <c>notificationRepository.Update(notification)</c>      — mark dirty
///     ⑤ <c>unitOfWork.SaveChangesAsync</c>                      — DB UPDATE
///
///   A compromised ally JWT (valid for 15 minutes) can hammer this endpoint in a
///   tight loop:
///     • Each iteration fires 2 SELECTs + 1 UPDATE → unbounded DB churn.
///     • The validator only checks that <c>ActionSummary</c> is non-empty and ≤ 280 chars;
///       it does not prevent repeated calls on the same notification.
///     • Because the notification state machine allows re-confirming the same
///       notification, each call succeeds and writes a new row version.
///
///   Active <c>notificationId</c> GUIDs are available in the ally's own alert inbox
///   (<c>GET /api/allies/me/alerts</c>), so the attack requires no external
///   enumeration — just a valid token and the notification list.
///
/// Fix:
///   Apply <c>[EnableRateLimiting("public-api")]</c> (30 req/min per IP) to
///   <c>AlliesController.ConfirmAction</c>.  The existing <c>public-api</c> policy
///   is already applied to <c>POST /api/allies/me/application</c> in the same
///   controller, making this a consistent throttle across all ally write endpoints.
///   30/min is more than sufficient for a volunteer confirming alert actions
///   while stopping the looped-write attack.
/// </summary>
public sealed class Round26SecurityRegressionTests
{
    // ── PUT /api/allies/me/alerts/{notificationId}/action ─────────────────────

    [Fact]
    public void AlliesController_ConfirmAction_HasEnableRateLimitingAttribute()
    {
        // ConfirmAllyAlertActionCommandHandler writes to the DB on every call
        // (SELECT × 2 + UPDATE + SaveChangesAsync).  Without [EnableRateLimiting]
        // a compromised ally JWT can generate unbounded DB write churn for the
        // duration of its 15-minute lifetime.
        var method = typeof(AlliesController)
            .GetMethod("ConfirmAction", BindingFlags.Public | BindingFlags.Instance);

        method.Should().NotBeNull(
            "AlliesController must expose a public ConfirmAction method");

        var attr = method!.GetCustomAttribute<EnableRateLimitingAttribute>();
        attr.Should().NotBeNull(
            "PUT /api/allies/me/alerts/{notificationId}/action must carry " +
            "[EnableRateLimiting] — the handler calls SaveChangesAsync() on every " +
            "invocation; without a rate limit a valid ally JWT can generate " +
            "unbounded DB write churn for the full 15-minute token lifetime");
    }

    [Fact]
    public void AlliesController_ConfirmAction_UsesPublicApiPolicy()
    {
        // Reuse the existing "public-api" policy (30/min) that is already applied
        // to POST /api/allies/me/application in the same controller — ensures
        // consistent throttling across all ally write endpoints.
        var method = typeof(AlliesController)
            .GetMethod("ConfirmAction", BindingFlags.Public | BindingFlags.Instance);

        var attr = method!.GetCustomAttribute<EnableRateLimitingAttribute>();
        attr.Should().NotBeNull();
        attr!.PolicyName.Should().Be(
            "public-api",
            "ConfirmAction must use the 'public-api' policy (30 req/min) for " +
            "consistency with POST /api/allies/me/application which already uses " +
            "the same policy; 30/min is generous for legitimate ally action " +
            "confirmations while stopping the looped-write attack");
    }
}
