using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PawTrack.API.Controllers;
using System.Reflection;

namespace PawTrack.UnitTests.Security;

/// <summary>
/// Round-34 security regression tests.
///
/// Gap: Nine read endpoints and two admin write endpoints across four controllers
/// are behind <c>[Authorize]</c> or <c>[Authorize(Roles = "Admin")]</c> but carry
/// <b>zero <c>[EnableRateLimiting]</c></b>:
///
/// ── <c>AlliesController</c> ──────────────────────────────────────────────────
///   • <c>GET  /api/allies/me</c>                          — <c>GetMyProfile</c>
///   • <c>GET  /api/allies/me/alerts</c>                   — <c>GetMyAlerts</c>
///   • <c>GET  /api/allies/admin/pending</c>               — <c>GetPendingApplications</c>
///   • <c>POST /api/allies/admin/applications/{id}/review</c> — <c>ReviewApplication</c>
///
/// ── <c>FostersController</c> ─────────────────────────────────────────────────
///   • <c>GET /api/fosters/me</c>                          — <c>GetMyProfile</c>
///
/// ── <c>ClinicsController</c> ─────────────────────────────────────────────────
///   • <c>GET /api/clinics/me</c>                          — <c>GetMyClinic</c>
///   • <c>GET /api/clinics/admin/pending</c>               — <c>GetPendingClinics</c>
///   • <c>PUT /api/clinics/admin/{id}/review</c>           — <c>ReviewClinic</c>
///
/// ── <c>IncentivesController</c> ──────────────────────────────────────────────
///   • <c>GET /api/incentives/my-score</c>                 — <c>GetMyScore</c>
///
/// ── Shared risk ───────────────────────────────────────────────────────────────
///   Every read endpoint listed above issues at least one DB query per call.
///   Without a rate limit, a valid JWT (15-min window) or an Admin JWT (same
///   window) can generate unbounded queries on the underlying tables.
///
///   The two admin write endpoints (<c>ReviewApplication</c> and
///   <c>ReviewClinic</c>) each call <c>SaveChangesAsync()</c>.  Although they
///   require the "Admin" role (which limits who can call them), an Admin account
///   whose credentials are compromised can still generate unbounded DB writes
///   without any per-endpoint throttle guarding the resource.
///
///   The <c>GetPendingApplications</c> and <c>GetPendingClinics</c> admin reads
///   perform full-table scans over the pending-application queue.  A compromised
///   or malicious Admin account can pump these repeatedly, exhausting DB
///   connection slots.
///
/// Fix:
///   Apply <c>[EnableRateLimiting("public-api")]</c> (30 req/min) to all nine
///   read endpoints.  Apply <c>[EnableRateLimiting("public-api")]</c> as well to
///   the two admin write endpoints — the "Admin" role restriction is an
///   authorization control; rate limiting is an independent resource-protection
///   control, and both must be present (defense in depth).
/// </summary>
public sealed class Round34SecurityRegressionTests
{
    // ── AlliesController.GetMyProfile ─────────────────────────────────────────

    [Fact]
    public void AlliesController_GetMyProfile_HasEnableRateLimitingAttribute()
    {
        var method = typeof(AlliesController)
            .GetMethod("GetMyProfile", BindingFlags.Public | BindingFlags.Instance);

        method.Should().NotBeNull("AlliesController must expose a public GetMyProfile method");

        method!.GetCustomAttribute<EnableRateLimitingAttribute>().Should().NotBeNull(
            "GET /api/allies/me must carry [EnableRateLimiting] — 1 DB read per call; " +
            "unthrottled polling exhausts DB connection slots");
    }

    [Fact]
    public void AlliesController_GetMyProfile_UsesPublicApiPolicy()
    {
        var attr = typeof(AlliesController)
            .GetMethod("GetMyProfile", BindingFlags.Public | BindingFlags.Instance)!
            .GetCustomAttribute<EnableRateLimitingAttribute>();
        attr.Should().NotBeNull();
        attr!.PolicyName.Should().Be("public-api");
    }

    // ── AlliesController.GetMyAlerts ──────────────────────────────────────────

    [Fact]
    public void AlliesController_GetMyAlerts_HasEnableRateLimitingAttribute()
    {
        var method = typeof(AlliesController)
            .GetMethod("GetMyAlerts", BindingFlags.Public | BindingFlags.Instance);

        method.Should().NotBeNull("AlliesController must expose a public GetMyAlerts method");

        method!.GetCustomAttribute<EnableRateLimitingAttribute>().Should().NotBeNull(
            "GET /api/allies/me/alerts must carry [EnableRateLimiting] — " +
            "1 DB read per call; without throttle a valid JWT can exhaust DB connections");
    }

    [Fact]
    public void AlliesController_GetMyAlerts_UsesPublicApiPolicy()
    {
        var attr = typeof(AlliesController)
            .GetMethod("GetMyAlerts", BindingFlags.Public | BindingFlags.Instance)!
            .GetCustomAttribute<EnableRateLimitingAttribute>();
        attr.Should().NotBeNull();
        attr!.PolicyName.Should().Be("public-api");
    }

    // ── AlliesController.GetPendingApplications ───────────────────────────────

    [Fact]
    public void AlliesController_GetPendingApplications_HasEnableRateLimitingAttribute()
    {
        // Full-table scan over the pending ally queue.
        // Admin role restricts WHO can call this, but not HOW MANY TIMES.
        var method = typeof(AlliesController)
            .GetMethod("GetPendingApplications", BindingFlags.Public | BindingFlags.Instance);

        method.Should().NotBeNull("AlliesController must expose a public GetPendingApplications method");

        method!.GetCustomAttribute<EnableRateLimitingAttribute>().Should().NotBeNull(
            "GET /api/allies/admin/pending must carry [EnableRateLimiting] — " +
            "full-table scan of the pending-applications queue; role restriction " +
            "is an authorization control, rate limiting is an independent resource " +
            "protection — both are required (defense in depth)");
    }

    [Fact]
    public void AlliesController_GetPendingApplications_UsesPublicApiPolicy()
    {
        var attr = typeof(AlliesController)
            .GetMethod("GetPendingApplications", BindingFlags.Public | BindingFlags.Instance)!
            .GetCustomAttribute<EnableRateLimitingAttribute>();
        attr.Should().NotBeNull();
        attr!.PolicyName.Should().Be("public-api");
    }

    // ── AlliesController.ReviewApplication ───────────────────────────────────

    [Fact]
    public void AlliesController_ReviewApplication_HasEnableRateLimitingAttribute()
    {
        // ReviewAllyApplicationCommand calls SaveChangesAsync on every invocation.
        // A compromised Admin JWT can generate unbounded DB writes.
        var method = typeof(AlliesController)
            .GetMethod("ReviewApplication", BindingFlags.Public | BindingFlags.Instance);

        method.Should().NotBeNull("AlliesController must expose a public ReviewApplication method");

        method!.GetCustomAttribute<EnableRateLimitingAttribute>().Should().NotBeNull(
            "POST /api/allies/admin/applications/{id}/review must carry [EnableRateLimiting] — " +
            "each call writes to DB via SaveChangesAsync; a compromised Admin JWT " +
            "can generate unbounded DB writes without a per-endpoint throttle");
    }

    [Fact]
    public void AlliesController_ReviewApplication_UsesPublicApiPolicy()
    {
        var attr = typeof(AlliesController)
            .GetMethod("ReviewApplication", BindingFlags.Public | BindingFlags.Instance)!
            .GetCustomAttribute<EnableRateLimitingAttribute>();
        attr.Should().NotBeNull();
        attr!.PolicyName.Should().Be("public-api");
    }

    // ── FostersController.GetMyProfile ────────────────────────────────────────

    [Fact]
    public void FostersController_GetMyProfile_HasEnableRateLimitingAttribute()
    {
        var method = typeof(FostersController)
            .GetMethod("GetMyProfile", BindingFlags.Public | BindingFlags.Instance);

        method.Should().NotBeNull("FostersController must expose a public GetMyProfile method");

        method!.GetCustomAttribute<EnableRateLimitingAttribute>().Should().NotBeNull(
            "GET /api/fosters/me must carry [EnableRateLimiting] — " +
            "1 DB read per call; consistent with other read endpoints on the " +
            "same controller which already carry [EnableRateLimiting]");
    }

    [Fact]
    public void FostersController_GetMyProfile_UsesPublicApiPolicy()
    {
        var attr = typeof(FostersController)
            .GetMethod("GetMyProfile", BindingFlags.Public | BindingFlags.Instance)!
            .GetCustomAttribute<EnableRateLimitingAttribute>();
        attr.Should().NotBeNull();
        attr!.PolicyName.Should().Be("public-api");
    }

    // ── ClinicsController.GetMyClinic ─────────────────────────────────────────

    [Fact]
    public void ClinicsController_GetMyClinic_HasEnableRateLimitingAttribute()
    {
        var method = typeof(ClinicsController)
            .GetMethod("GetMyClinic", BindingFlags.Public | BindingFlags.Instance);

        method.Should().NotBeNull("ClinicsController must expose a public GetMyClinic method");

        method!.GetCustomAttribute<EnableRateLimitingAttribute>().Should().NotBeNull(
            "GET /api/clinics/me must carry [EnableRateLimiting] — " +
            "1 DB read per call; Clinic-role credential compromise allows " +
            "unbounded polling without a throttle");
    }

    [Fact]
    public void ClinicsController_GetMyClinic_UsesPublicApiPolicy()
    {
        var attr = typeof(ClinicsController)
            .GetMethod("GetMyClinic", BindingFlags.Public | BindingFlags.Instance)!
            .GetCustomAttribute<EnableRateLimitingAttribute>();
        attr.Should().NotBeNull();
        attr!.PolicyName.Should().Be("public-api");
    }

    // ── ClinicsController.GetPendingClinics ───────────────────────────────────

    [Fact]
    public void ClinicsController_GetPendingClinics_HasEnableRateLimitingAttribute()
    {
        // Full-table scan of the pending clinics queue, Admin-only.
        var method = typeof(ClinicsController)
            .GetMethod("GetPendingClinics", BindingFlags.Public | BindingFlags.Instance);

        method.Should().NotBeNull("ClinicsController must expose a public GetPendingClinics method");

        method!.GetCustomAttribute<EnableRateLimitingAttribute>().Should().NotBeNull(
            "GET /api/clinics/admin/pending must carry [EnableRateLimiting] — " +
            "full-table scan of pending clinics queue; Admin role does not " +
            "substitute for a per-endpoint throttle (defense in depth)");
    }

    [Fact]
    public void ClinicsController_GetPendingClinics_UsesPublicApiPolicy()
    {
        var attr = typeof(ClinicsController)
            .GetMethod("GetPendingClinics", BindingFlags.Public | BindingFlags.Instance)!
            .GetCustomAttribute<EnableRateLimitingAttribute>();
        attr.Should().NotBeNull();
        attr!.PolicyName.Should().Be("public-api");
    }

    // ── ClinicsController.ReviewClinic ────────────────────────────────────────

    [Fact]
    public void ClinicsController_ReviewClinic_HasEnableRateLimitingAttribute()
    {
        // ReviewClinicCommand calls SaveChangesAsync.
        var method = typeof(ClinicsController)
            .GetMethod("ReviewClinic", BindingFlags.Public | BindingFlags.Instance);

        method.Should().NotBeNull("ClinicsController must expose a public ReviewClinic method");

        method!.GetCustomAttribute<EnableRateLimitingAttribute>().Should().NotBeNull(
            "PUT /api/clinics/admin/{id}/review must carry [EnableRateLimiting] — " +
            "each call writes to DB; a compromised Admin JWT generates unbounded " +
            "DB writes without a per-endpoint throttle");
    }

    [Fact]
    public void ClinicsController_ReviewClinic_UsesPublicApiPolicy()
    {
        var attr = typeof(ClinicsController)
            .GetMethod("ReviewClinic", BindingFlags.Public | BindingFlags.Instance)!
            .GetCustomAttribute<EnableRateLimitingAttribute>();
        attr.Should().NotBeNull();
        attr!.PolicyName.Should().Be("public-api");
    }

    // ── IncentivesController.GetMyScore ──────────────────────────────────────

    [Fact]
    public void IncentivesController_GetMyScore_HasEnableRateLimitingAttribute()
    {
        // The sibling GetLeaderboard already has [EnableRateLimiting("public-api")].
        // Leaving GetMyScore unthrottled creates asymmetric protection on the
        // same resource (OWASP A05).
        var method = typeof(IncentivesController)
            .GetMethod("GetMyScore", BindingFlags.Public | BindingFlags.Instance);

        method.Should().NotBeNull("IncentivesController must expose a public GetMyScore method");

        method!.GetCustomAttribute<EnableRateLimitingAttribute>().Should().NotBeNull(
            "GET /api/incentives/my-score must carry [EnableRateLimiting] — " +
            "its sibling GET /api/incentives/leaderboard already uses 'public-api'; " +
            "leaving the authenticated read unthrottled creates asymmetric " +
            "protection on the same resource (OWASP A05: Security Misconfiguration)");
    }

    [Fact]
    public void IncentivesController_GetMyScore_UsesPublicApiPolicy()
    {
        var attr = typeof(IncentivesController)
            .GetMethod("GetMyScore", BindingFlags.Public | BindingFlags.Instance)!
            .GetCustomAttribute<EnableRateLimitingAttribute>();
        attr.Should().NotBeNull();
        attr!.PolicyName.Should().Be("public-api");
    }
}
