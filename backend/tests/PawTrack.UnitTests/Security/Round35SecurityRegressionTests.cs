using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using PawTrack.API.Controllers;
using System.Reflection;

namespace PawTrack.UnitTests.Security;

/// <summary>
/// Round-35 security regression tests.
///
/// Gap: Eight write endpoints across five controllers accept <c>[FromBody]</c>
/// payloads but carry no <c>[RequestSizeLimit]</c>.  Kestrel's global cap
/// is 1 MB (set in Program.cs), but that is far too generous for endpoints whose
/// legitimate payloads are measured in bytes or a few hundred characters:
///
/// ── <c>FraudReportController</c> ─────────────────────────────────────────────
///   • <c>POST /api/fraud-reports</c>          — <c>ReportFraud</c>
///     Body: context enum + two optional GUIDs + description ≤ 500 chars.
///     At most ~600 bytes; 4 KB ceiling prevents 1 MB JSON blobs from triggering
///     full deserialization + FluentValidation + DB write.
///
/// ── <c>HandoverController</c> ────────────────────────────────────────────────
///   • <c>POST /api/lost-pets/{id}/handover/verify</c> — <c>VerifyCode</c>
///     Body: 4-digit string literal. 512 B is generous headroom.
///
/// ── <c>LocationsController</c> ───────────────────────────────────────────────
///   • <c>PUT /api/me/location</c>             — <c>UpsertLocation</c>
///     Body: lat/lng doubles + bool + two nullable TimeOnly fields.
///     4 KB covers the full JSON with whitespace to spare.
///
/// ── <c>NotificationsController</c> ───────────────────────────────────────────
///   • <c>PUT /api/notifications/preferences</c>     — <c>UpdatePreferences</c>
///     Body: single bool. 512 B is more than enough.
///   • <c>POST /api/notifications/push-subscription</c> — <c>RegisterPushSubscription</c>
///     Body: endpoint URL + VAPID keys JSON. 8 KB covers the largest real-world
///     Web Push subscription object.
///
/// ── <c>ClinicsController</c> ─────────────────────────────────────────────────
///   • <c>POST /api/clinics/register</c>       — <c>Register</c>
///     Body: name + licenseNumber + address + email + password. 4 KB ceiling.
///   • <c>POST /api/clinics/scan</c>           — <c>Scan</c>
///     Body: input string + input-type enum. 2 KB ceiling.
///   • <c>PUT  /api/clinics/admin/{id}/review</c> — <c>ReviewClinic</c>
///     Body: single bool (Approve). 512 B ceiling.
///
/// ── Shared risk ───────────────────────────────────────────────────────────────
///   Without a per-action <c>[RequestSizeLimit]</c>, a single request carrying a
///   payload close to the global 1 MB limit causes the ASP.NET runtime to buffer
///   and then fully deserialize the body before any FluentValidation pipeline
///   behavior fires.  For endpoints whose handlers call <c>SaveChangesAsync</c>
///   (VerifyCode writes a HandoverAttempt row; UpsertLocation writes GPS PII;
///   ReportFraud triggers SuspicionLevel computation + audit write) this creates
///   a CPU + DB write amplification vector.
///
/// Fix:
///   Apply <c>[RequestSizeLimit(n)]</c> at the action level:
///     ReportFraud          → 4 096 B (4 KB)
///     VerifyCode           →   512 B
///     UpsertLocation       → 4 096 B (4 KB)
///     UpdatePreferences    →   512 B
///     RegisterPushSub      → 8 192 B (8 KB)
///     Register (clinic)    → 4 096 B (4 KB)
///     Scan                 → 2 048 B (2 KB)
///     ReviewClinic         →   512 B
/// </summary>
public sealed class Round35SecurityRegressionTests
{
    // ── FraudReportController.ReportFraud ────────────────────────────────────

    [Fact]
    public void FraudReportController_ReportFraud_HasRequestSizeLimitAttribute()
    {
        var method = typeof(FraudReportController)
            .GetMethod("ReportFraud", BindingFlags.Public | BindingFlags.Instance);

        method.Should().NotBeNull("FraudReportController must expose a public ReportFraud method");

        var attr = method!.GetCustomAttribute<Microsoft.AspNetCore.Mvc.RequestSizeLimitAttribute>();
        attr.Should().NotBeNull(
            "POST /api/fraud-reports accepts description ≤ 500 chars; a missing [RequestSizeLimit] " +
            "allows 1 MB JSON bodies that trigger full deserialization + SuspicionLevel computation + DB write");
    }

    // ── HandoverController.VerifyCode ────────────────────────────────────────

    [Fact]
    public void HandoverController_VerifyCode_HasRequestSizeLimitAttribute()
    {
        var method = typeof(HandoverController)
            .GetMethod("VerifyCode", BindingFlags.Public | BindingFlags.Instance);

        method.Should().NotBeNull("HandoverController must expose a public VerifyCode method");

        var attr = method!.GetCustomAttribute<Microsoft.AspNetCore.Mvc.RequestSizeLimitAttribute>();
        attr.Should().NotBeNull(
            "POST /api/lost-pets/{id}/handover/verify accepts only a 4-digit code; " +
            "a missing [RequestSizeLimit] allows 1 MB payloads against a rate-limited brute-force endpoint");
    }

    // ── LocationsController.UpsertLocation ───────────────────────────────────

    [Fact]
    public void LocationsController_UpsertLocation_HasRequestSizeLimitAttribute()
    {
        var method = typeof(LocationsController)
            .GetMethod("UpsertLocation", BindingFlags.Public | BindingFlags.Instance);

        method.Should().NotBeNull("LocationsController must expose a public UpsertLocation method");

        var attr = method!.GetCustomAttribute<Microsoft.AspNetCore.Mvc.RequestSizeLimitAttribute>();
        attr.Should().NotBeNull(
            "PUT /api/me/location writes GPS PII (lat/lng) + quiet-hours preferences on every call; " +
            "a missing [RequestSizeLimit] allows 1 MB payloads to reach SaveChangesAsync");
    }

    // ── NotificationsController.UpdatePreferences ────────────────────────────

    [Fact]
    public void NotificationsController_UpdatePreferences_HasRequestSizeLimitAttribute()
    {
        var method = typeof(NotificationsController)
            .GetMethod("UpdatePreferences", BindingFlags.Public | BindingFlags.Instance);

        method.Should().NotBeNull("NotificationsController must expose a public UpdatePreferences method");

        var attr = method!.GetCustomAttribute<Microsoft.AspNetCore.Mvc.RequestSizeLimitAttribute>();
        attr.Should().NotBeNull(
            "PUT /api/notifications/preferences body is a single bool; " +
            "a missing [RequestSizeLimit] allows 1 MB payloads against writes to the DB preferences row");
    }

    // ── NotificationsController.RegisterPushSubscription ─────────────────────

    [Fact]
    public void NotificationsController_RegisterPushSubscription_HasRequestSizeLimitAttribute()
    {
        var method = typeof(NotificationsController)
            .GetMethod("RegisterPushSubscription", BindingFlags.Public | BindingFlags.Instance);

        method.Should().NotBeNull("NotificationsController must expose a public RegisterPushSubscription method");

        var attr = method!.GetCustomAttribute<Microsoft.AspNetCore.Mvc.RequestSizeLimitAttribute>();
        attr.Should().NotBeNull(
            "POST /api/notifications/push-subscription stores a Web Push endpoint URL + VAPID keys; " +
            "a missing [RequestSizeLimit] allows 1 MB bodies to reach the DB insert");
    }

    // ── ClinicsController.Register ───────────────────────────────────────────

    [Fact]
    public void ClinicsController_Register_HasRequestSizeLimitAttribute()
    {
        var method = typeof(ClinicsController)
            .GetMethod("Register", BindingFlags.Public | BindingFlags.Instance);

        method.Should().NotBeNull("ClinicsController must expose a public Register method");

        var attr = method!.GetCustomAttribute<Microsoft.AspNetCore.Mvc.RequestSizeLimitAttribute>();
        attr.Should().NotBeNull(
            "POST /api/clinics/register creates a new clinic user account; " +
            "a missing [RequestSizeLimit] allows 1 MB bodies to reach the RegisterClinicCommand handler");
    }

    // ── ClinicsController.Scan ───────────────────────────────────────────────

    [Fact]
    public void ClinicsController_Scan_HasRequestSizeLimitAttribute()
    {
        var method = typeof(ClinicsController)
            .GetMethod("Scan", BindingFlags.Public | BindingFlags.Instance);

        method.Should().NotBeNull("ClinicsController must expose a public Scan method");

        var attr = method!.GetCustomAttribute<Microsoft.AspNetCore.Mvc.RequestSizeLimitAttribute>();
        attr.Should().NotBeNull(
            "POST /api/clinics/scan accepts an input string + enum; " +
            "a missing [RequestSizeLimit] allows 1 MB payloads to trigger PerformClinicScanCommand + owner notification dispatch");
    }

    // ── ClinicsController.ReviewClinic ───────────────────────────────────────

    [Fact]
    public void ClinicsController_ReviewClinic_HasRequestSizeLimitAttribute()
    {
        // ReviewClinic is the HttpPut admin endpoint — GetMethod returns the correct overload
        // because there is only one method named ReviewClinic in ClinicsController.
        var method = typeof(ClinicsController)
            .GetMethod("ReviewClinic", BindingFlags.Public | BindingFlags.Instance);

        method.Should().NotBeNull("ClinicsController must expose a public ReviewClinic method");

        var attr = method!.GetCustomAttribute<Microsoft.AspNetCore.Mvc.RequestSizeLimitAttribute>();
        attr.Should().NotBeNull(
            "PUT /api/clinics/admin/{id}/review body is a single bool (Approve); " +
            "a missing [RequestSizeLimit] allows 1 MB bodies against a DB-write admin action");
    }
}
