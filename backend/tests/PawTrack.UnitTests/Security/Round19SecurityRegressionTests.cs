using FluentAssertions;
using Microsoft.AspNetCore.RateLimiting;
using PawTrack.API.Controllers;
using System.Reflection;

namespace PawTrack.UnitTests.Security;

/// <summary>
/// Round-19 security regression tests.
///
/// Gap: <c>POST /api/clinics/scan</c> has no rate limiting.
/// Every other writable endpoint in the API carries an <c>[EnableRateLimiting]</c>
/// attribute; the clinic scan endpoint was skipped.
///
/// Attack vector:
///   1. An attacker obtains (or compromises) an approved clinic account.
///      Clinic registration requires admin approval only once — after that the
///      JWT can be used indefinitely until revoked.
///   2. The attacker drives an unlimited number of requests per second to
///      <c>POST /api/clinics/scan</c>.
///   3. For each scan the server:
///      ① Persists a <c>ClinicScan</c> audit record to the SQL database.
///      ② Dispatches a "your pet was scanned" push + email notification to the
///         pet owner via <c>DispatchClinicScanDetectedAsync</c>.
///   4. Consequences:
///      - Database table grows unboundedly (storage DoS).
///      - Pet owners are flooded with notification emails (notification spam /
///        operational DoS on the notification service).
///      - Azure notification / email quotas are exhausted — prevents legitimate
///        lost-pet alerts from being delivered.
///
/// Fix: Add <c>[EnableRateLimiting("clinic-scan")]</c> to the Scan method and
/// register a <c>clinic-scan</c> fixed-window policy (30 scans/min) in
/// <c>Program.cs</c>.  30/min is compatible with real clinic workloads while
/// making systematic enumeration impractical.
/// </summary>
public sealed class Round19SecurityRegressionTests
{
    // ── ClinicsController.Scan — rate-limit attribute ─────────────────────────

    [Fact]
    public void ClinicsController_Scan_HasEnableRateLimitingAttribute()
    {
        // Without [EnableRateLimiting], UseRateLimiter() middleware does not
        // throttle the endpoint regardless of the policies registered in Program.cs.
        var method = typeof(ClinicsController)
            .GetMethod("Scan", BindingFlags.Public | BindingFlags.Instance);

        method.Should().NotBeNull("ClinicsController must have a public Scan method");

        var attr = method!.GetCustomAttribute<EnableRateLimitingAttribute>();

        attr.Should().NotBeNull(
            "POST /api/clinics/scan must carry [EnableRateLimiting] — " +
            "without it a single clinic account can issue unlimited scans, " +
            "flooding the database and spamming pet owners with notifications");
    }

    [Fact]
    public void ClinicsController_Scan_UsesClinicScanPolicy()
    {
        // The policy name must match the one registered in Program.cs; a typo
        // makes the middleware silently skip rate limiting.
        var method = typeof(ClinicsController)
            .GetMethod("Scan", BindingFlags.Public | BindingFlags.Instance);

        var attr = method!.GetCustomAttribute<EnableRateLimitingAttribute>();

        attr.Should().NotBeNull();
        attr!.PolicyName.Should().Be(
            "clinic-scan",
            "the rate-limit policy for the scan endpoint must be named 'clinic-scan' " +
            "to match the policy registered in Program.cs");
    }
}
