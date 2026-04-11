using FluentAssertions;
using Microsoft.AspNetCore.RateLimiting;
using PawTrack.API.Controllers;
using System.Reflection;

namespace PawTrack.UnitTests.Security;

/// <summary>
/// Round-22 security regression tests.
///
/// Gap: <c>NotificationsController</c> has <c>[Authorize]</c> at class level but
/// carries <b>zero <c>[EnableRateLimiting]</c> attributes</b> on any of its
/// seven endpoints.
///
/// Attack vector:
///   1. An attacker obtains a valid JWT (e.g., via credential stuffing or a
///      stolen refresh token before the round-3 theft-detection fix fires).
///      A 15-minute access token window is enough to sustain a flood.
///   2. The attacker hammers <c>POST /api/notifications/push-subscription</c> in
///      a tight loop.  Each call executes:
///         ① <c>GetByEndpointAsync</c>  — DB read (index scan on endpoint column)
///         ② <c>DeleteByEndpointAsync</c> — DB write (conditional DELETE)
///         ③ <c>AddAsync</c>            — DB write (INSERT)
///         ④ <c>SaveChangesAsync</c>    — transaction flush
///      Four round-trips per request, unbounded.
///   3. Additional targets in the same controller:
///      - <c>PUT /read-all</c>: bulk UPDATE on all notifications for the user.
///      - <c>PUT /preferences</c>: UPDATE on notification preferences row.
///      - <c>POST /{id}/resolve-check-response</c>: triggers domain state
///        machine, dispatches cross-module MediatR notifications.
///
/// Fix:
///   • Register a new <c>"notifications-write"</c> fixed-window policy in
///     <c>Program.cs</c> (20 req/min per partition — generous for human use,
///     stops automated hammering).
///   • Apply <c>[EnableRateLimiting("notifications-write")]</c> to all five
///     write methods:
///       - RegisterPushSubscription, UpdatePreferences, MarkAsRead, MarkAllAsRead,
///         RespondResolveCheck
///   • Apply <c>[EnableRateLimiting("public-api")]</c> to the two read methods:
///       - GetMyNotifications, GetPreferences
/// </summary>
public sealed class Round22SecurityRegressionTests
{
    // ── POST /api/notifications/push-subscription ─────────────────────────────

    [Fact]
    public void NotificationsController_RegisterPushSubscription_HasEnableRateLimitingAttribute()
    {
        // Each call does DELETE + INSERT on push_subscriptions.
        // Without rate limiting a single compromised JWT can churn the table
        // at thousands of writes/second, causing storage DoS and DB CPU spike.
        var method = typeof(NotificationsController)
            .GetMethod("RegisterPushSubscription", BindingFlags.Public | BindingFlags.Instance);

        method.Should().NotBeNull(
            "NotificationsController must expose a public RegisterPushSubscription method");

        var attr = method!.GetCustomAttribute<EnableRateLimitingAttribute>();
        attr.Should().NotBeNull(
            "POST /api/notifications/push-subscription must carry [EnableRateLimiting] — " +
            "each call executes a DELETE + INSERT on the push_subscriptions table; " +
            "without a rate limit a single JWT can churn the table indefinitely");
    }

    [Fact]
    public void NotificationsController_RegisterPushSubscription_UsesNotificationsWritePolicy()
    {
        var method = typeof(NotificationsController)
            .GetMethod("RegisterPushSubscription", BindingFlags.Public | BindingFlags.Instance);

        var attr = method!.GetCustomAttribute<EnableRateLimitingAttribute>();
        attr.Should().NotBeNull();
        attr!.PolicyName.Should().Be(
            "notifications-write",
            "the rate-limit policy for push-subscription registration must be " +
            "'notifications-write' to match the policy registered in Program.cs");
    }

    // ── PUT /api/notifications/preferences ────────────────────────────────────

    [Fact]
    public void NotificationsController_UpdatePreferences_HasEnableRateLimitingAttribute()
    {
        var method = typeof(NotificationsController)
            .GetMethod("UpdatePreferences", BindingFlags.Public | BindingFlags.Instance);

        method.Should().NotBeNull(
            "NotificationsController must expose a public UpdatePreferences method");

        var attr = method!.GetCustomAttribute<EnableRateLimitingAttribute>();
        attr.Should().NotBeNull(
            "PUT /api/notifications/preferences must carry [EnableRateLimiting] — " +
            "without it a loop of preference updates generates unbounded DB writes " +
            "from a single compromised account");
    }

    [Fact]
    public void NotificationsController_UpdatePreferences_UsesNotificationsWritePolicy()
    {
        var method = typeof(NotificationsController)
            .GetMethod("UpdatePreferences", BindingFlags.Public | BindingFlags.Instance);

        var attr = method!.GetCustomAttribute<EnableRateLimitingAttribute>();
        attr.Should().NotBeNull();
        attr!.PolicyName.Should().Be("notifications-write");
    }

    // ── PUT /api/notifications/read-all ──────────────────────────────────────

    [Fact]
    public void NotificationsController_MarkAllAsRead_HasEnableRateLimitingAttribute()
    {
        var method = typeof(NotificationsController)
            .GetMethod("MarkAllAsRead", BindingFlags.Public | BindingFlags.Instance);

        method.Should().NotBeNull(
            "NotificationsController must expose a public MarkAllAsRead method");

        var attr = method!.GetCustomAttribute<EnableRateLimitingAttribute>();
        attr.Should().NotBeNull(
            "PUT /api/notifications/read-all must carry [EnableRateLimiting] — " +
            "each call issues a bulk UPDATE; repeated calls cause unnecessary " +
            "write amplification on the notifications table");
    }

    [Fact]
    public void NotificationsController_MarkAllAsRead_UsesNotificationsWritePolicy()
    {
        var method = typeof(NotificationsController)
            .GetMethod("MarkAllAsRead", BindingFlags.Public | BindingFlags.Instance);

        var attr = method!.GetCustomAttribute<EnableRateLimitingAttribute>();
        attr.Should().NotBeNull();
        attr!.PolicyName.Should().Be("notifications-write");
    }

    // ── PUT /api/notifications/{id}/read ─────────────────────────────────────

    [Fact]
    public void NotificationsController_MarkAsRead_HasEnableRateLimitingAttribute()
    {
        var method = typeof(NotificationsController)
            .GetMethod("MarkAsRead", BindingFlags.Public | BindingFlags.Instance);

        method.Should().NotBeNull(
            "NotificationsController must expose a public MarkAsRead method");

        var attr = method!.GetCustomAttribute<EnableRateLimitingAttribute>();
        attr.Should().NotBeNull(
            "PUT /api/notifications/{id}/read must carry [EnableRateLimiting] — " +
            "rapid cycling across notification IDs causes unbounded DB writes " +
            "from a compromised account");
    }

    [Fact]
    public void NotificationsController_MarkAsRead_UsesNotificationsWritePolicy()
    {
        var method = typeof(NotificationsController)
            .GetMethod("MarkAsRead", BindingFlags.Public | BindingFlags.Instance);

        var attr = method!.GetCustomAttribute<EnableRateLimitingAttribute>();
        attr.Should().NotBeNull();
        attr!.PolicyName.Should().Be("notifications-write");
    }

    // ── POST /api/notifications/{id}/resolve-check-response ──────────────────

    [Fact]
    public void NotificationsController_RespondResolveCheck_HasEnableRateLimitingAttribute()
    {
        var method = typeof(NotificationsController)
            .GetMethod("RespondResolveCheck", BindingFlags.Public | BindingFlags.Instance);

        method.Should().NotBeNull(
            "NotificationsController must expose a public RespondResolveCheck method");

        var attr = method!.GetCustomAttribute<EnableRateLimitingAttribute>();
        attr.Should().NotBeNull(
            "POST /api/notifications/{id}/resolve-check-response must carry [EnableRateLimiting] — " +
            "each call triggers a domain state machine and may dispatch cross-module " +
            "MediatR notifications; without rate limiting a compromised account can " +
            "spam the domain state machine indefinitely");
    }

    [Fact]
    public void NotificationsController_RespondResolveCheck_UsesNotificationsWritePolicy()
    {
        var method = typeof(NotificationsController)
            .GetMethod("RespondResolveCheck", BindingFlags.Public | BindingFlags.Instance);

        var attr = method!.GetCustomAttribute<EnableRateLimitingAttribute>();
        attr.Should().NotBeNull();
        attr!.PolicyName.Should().Be("notifications-write");
    }
}
