using FluentAssertions;
using Microsoft.AspNetCore.RateLimiting;
using PawTrack.API.Controllers;
using System.Reflection;

namespace PawTrack.UnitTests.Security;

/// <summary>
/// Round-30 security regression tests.
///
/// Gap: Two <c>GET</c> endpoints on <c>NotificationsController</c> carry
/// <c>[Authorize]</c> but <b>zero <c>[EnableRateLimiting]</c></b>:
///
///   • <c>GET /api/notifications</c>             — <c>GetMyNotifications</c>
///   • <c>GET /api/notifications/preferences</c> — <c>GetPreferences</c>
///
/// ── Attack 1: GetMyNotifications — double DB query, uncapped ──────────────────
///   <c>GetMyNotificationsQueryHandler</c> fires <b>two</b> DB queries on every call:
///     ① <c>notificationRepository.GetByUserIdAsync</c>   — paginated SELECT
///     ② <c>notificationRepository.CountUnreadAsync</c>   — COUNT query for badge
///   Without a rate limit, a tight loop with a valid JWT generates unbounded
///   paired DB reads.  The page parameter is clamped at 50, but the handler does
///   not limit the interval between calls — a mobile client caught in a polling
///   loop (or a compromised JWT) can exhaust the DB connection pool within the
///   15-minute access-token window.
///
/// ── Attack 2: GetPreferences — unthrottled read ───────────────────────────────
///   <c>GetNotificationPreferencesQuery</c> fires one DB query per call.  The
///   endpoint is the sibling of <c>PUT /api/notifications/preferences</c> which
///   already carries <c>[EnableRateLimiting("notifications-write")]</c>, yet
///   the read side is unthrottled.  A tight loop leaks whether
///   <c>EnablePreventiveAlerts</c> is toggled without any DB cost constraint.
///
/// Fix:
///   Apply <c>[EnableRateLimiting("public-api")]</c> (30 req/min per user) to
///   both GET endpoints.  The <c>public-api</c> policy is the standard read
///   throttle already applied to leaderboard, stats, and map endpoints; applying
///   it here makes the <c>NotificationsController</c> consistent.  The write
///   endpoints already use <c>"notifications-write"</c> (20/min) — keeping GET
///   and PUT on separate policies preserves the semantic separation between reads
///   and writes.
/// </summary>
public sealed class Round30SecurityRegressionTests
{
    // ── GET /api/notifications ── GetMyNotifications ──────────────────────────

    [Fact]
    public void NotificationsController_GetMyNotifications_HasEnableRateLimitingAttribute()
    {
        // GetMyNotificationsQueryHandler fires 2 DB queries per call.
        // Without [EnableRateLimiting] a polling loop or compromised JWT exhausts
        // the DB connection pool within the 15-minute access-token window.
        var method = typeof(NotificationsController)
            .GetMethod("GetMyNotifications", BindingFlags.Public | BindingFlags.Instance);

        method.Should().NotBeNull(
            "NotificationsController must expose a public GetMyNotifications method");

        var attr = method!.GetCustomAttribute<EnableRateLimitingAttribute>();
        attr.Should().NotBeNull(
            "GET /api/notifications must carry [EnableRateLimiting] — the handler " +
            "fires TWO DB queries per call (paginated SELECT + COUNT); without " +
            "throttling a polling loop or compromised JWT can exhaust the DB " +
            "connection pool within the 15-minute token window");
    }

    [Fact]
    public void NotificationsController_GetMyNotifications_UsesPublicApiPolicy()
    {
        var method = typeof(NotificationsController)
            .GetMethod("GetMyNotifications", BindingFlags.Public | BindingFlags.Instance);

        var attr = method!.GetCustomAttribute<EnableRateLimitingAttribute>();
        attr.Should().NotBeNull();
        attr!.PolicyName.Should().Be(
            "public-api",
            "GetMyNotifications must use the 'public-api' policy (30 req/min) — " +
            "semantic separation: GET endpoints use 'public-api' while write " +
            "endpoints on the same controller use 'notifications-write'");
    }

    // ── GET /api/notifications/preferences ── GetPreferences ─────────────────

    [Fact]
    public void NotificationsController_GetPreferences_HasEnableRateLimitingAttribute()
    {
        // PUT /api/notifications/preferences already has [EnableRateLimiting("notifications-write")].
        // The read side must be equivalently throttled — asymmetric protection on
        // the same resource is a security misconfiguration (OWASP A05).
        var method = typeof(NotificationsController)
            .GetMethod("GetPreferences", BindingFlags.Public | BindingFlags.Instance);

        method.Should().NotBeNull(
            "NotificationsController must expose a public GetPreferences method");

        var attr = method!.GetCustomAttribute<EnableRateLimitingAttribute>();
        attr.Should().NotBeNull(
            "GET /api/notifications/preferences must carry [EnableRateLimiting] — " +
            "its sibling PUT /api/notifications/preferences already uses " +
            "'notifications-write'; leaving the read side unthrottled creates " +
            "asymmetric protection on the same resource (OWASP A05)");
    }

    [Fact]
    public void NotificationsController_GetPreferences_UsesPublicApiPolicy()
    {
        var method = typeof(NotificationsController)
            .GetMethod("GetPreferences", BindingFlags.Public | BindingFlags.Instance);

        var attr = method!.GetCustomAttribute<EnableRateLimitingAttribute>();
        attr.Should().NotBeNull();
        attr!.PolicyName.Should().Be(
            "public-api",
            "GetPreferences must use the 'public-api' policy (30 req/min) — " +
            "read endpoints use 'public-api' while the write sibling uses " +
            "'notifications-write'; consistent policy assignment by HTTP verb");
    }
}
