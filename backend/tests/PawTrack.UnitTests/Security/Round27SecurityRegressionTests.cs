using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PawTrack.API.Controllers;
using System.Reflection;

namespace PawTrack.UnitTests.Security;

/// <summary>
/// Round-27 security regression tests.
///
/// Gap: <c>FostersController</c> has three write endpoints with <b>zero</b>
/// <c>[EnableRateLimiting]</c> and <b>zero</b> <c>[RequestSizeLimit]</c>:
///
///   • <c>PUT  /api/fosters/me</c>                     — <c>UpsertMyProfile</c>
///   • <c>POST /api/fosters/custody-records/start</c>  — <c>StartCustody</c>
///   • <c>PATCH /api/fosters/custody-records/{id}/close</c> — <c>CloseCustody</c>
///
/// ── Attack 1: PII home-coordinate churn (UpsertMyProfile) ────────────────────
///   <c>UpsertMyProfileCommand</c> persists the caller's precise GPS home address
///   (<c>HomeLat</c>, <c>HomeLng</c>) unconditionally on every call.  Without
///   rate limiting a compromised JWT can:
///     ① Continuously overwrite the foster's home address with adversarial GPS values
///        (GPS poisoning — corrupt the geo-matching index for foster suggestions).
///     ② Generate unlimited DB UPDATEs containing PII (exact home coordinates)
///        for the duration of the 15-minute access token.
///
/// ── Attack 2: Custody-record flooding (StartCustody) ─────────────────────────
///   <c>StartCustodyCommand</c> creates a new <c>CustodyRecord</c> row on every call.
///   Without rate limiting a compromised account can create thousands of phantom
///   custody records, polluting the foster-match index and exhausting storage.
///
/// ── Attack 3: State-machine cycling (CloseCustody) ────────────────────────────
///   <c>CloseCustodyCommand</c> transitions a custody record through its domain
///   state machine and writes to the DB.  Without rate limiting an attacker can
///   cycle the state machine repeatedly, generating unbounded DB writes.
///
/// ── Missing size limits ───────────────────────────────────────────────────────
///   None of the three endpoints carry <c>[RequestSizeLimit]</c>.  The largest
///   field is <c>Note</c> in <c>StartCustodyCommand</c> (validated ≤ 500 chars
///   by <c>StartCustodyCommandValidator</c>), but the request body is unbounded
///   at the HTTP layer.  A single oversized JSON payload can cause OOM in the
///   request-body parser before FluentValidation even runs.
///
/// Fix:
///   Apply <c>[EnableRateLimiting("public-api")]</c> (30 req/min) to all three
///   methods, and apply <c>[RequestSizeLimit]</c> matching the maximum expected
///   payload (<c>UpsertMyProfile</c>: 8 192 B; <c>StartCustody</c>: 4 096 B;
///   <c>CloseCustody</c>: 2 048 B).
/// </summary>
public sealed class Round27SecurityRegressionTests
{
    // ── PUT /api/fosters/me ── UpsertMyProfile ────────────────────────────────

    [Fact]
    public void FostersController_UpsertMyProfile_HasEnableRateLimitingAttribute()
    {
        // UpsertMyProfileCommand writes HomeLat + HomeLng (precise home address PII)
        // to the DB on every call.  Without [EnableRateLimiting] a compromised JWT
        // can generate unbounded GPS-coordinate writes + index corruption.
        var method = typeof(FostersController)
            .GetMethod("UpsertMyProfile", BindingFlags.Public | BindingFlags.Instance);

        method.Should().NotBeNull(
            "FostersController must expose a public UpsertMyProfile method");

        var attr = method!.GetCustomAttribute<EnableRateLimitingAttribute>();
        attr.Should().NotBeNull(
            "PUT /api/fosters/me must carry [EnableRateLimiting] — the command " +
            "writes PII home GPS coordinates to the DB on every call; without " +
            "throttling a compromised JWT can poison the foster geo-index and " +
            "generate unbounded PII writes for the full 15-minute token lifetime");
    }

    [Fact]
    public void FostersController_UpsertMyProfile_UsesPublicApiPolicy()
    {
        var method = typeof(FostersController)
            .GetMethod("UpsertMyProfile", BindingFlags.Public | BindingFlags.Instance);

        var attr = method!.GetCustomAttribute<EnableRateLimitingAttribute>();
        attr.Should().NotBeNull();
        attr!.PolicyName.Should().Be(
            "public-api",
            "UpsertMyProfile must use the 'public-api' policy (30 req/min) — " +
            "consistent with GET /api/fosters/suggestions which already applies " +
            "the same policy; 30/min safely covers any legitimate update pattern " +
            "while making PII-churn attacks impractical");
    }

    [Fact]
    public void FostersController_UpsertMyProfile_HasRequestSizeLimitAttribute()
    {
        // Body: JSON with two doubles (lat/lng), IReadOnlyList<PetSpecies> (enum array),
        // optional string, one int, one bool, optional DateTimeOffset.
        // 8 192 B ceiling stops OOM from oversized payloads before FluentValidation runs.
        var method = typeof(FostersController)
            .GetMethod("UpsertMyProfile", BindingFlags.Public | BindingFlags.Instance);

        method.Should().NotBeNull();

        var attr = method!.GetCustomAttribute<RequestSizeLimitAttribute>();
        attr.Should().NotBeNull(
            "PUT /api/fosters/me must carry [RequestSizeLimit] — the body " +
            "contains GPS coordinates and an enum list; without an HTTP-layer " +
            "size cap an oversized payload can trigger OOM in the body parser " +
            "before FluentValidation has a chance to reject it");
    }

    // ── POST /api/fosters/custody-records/start ── StartCustody ──────────────

    [Fact]
    public void FostersController_StartCustody_HasEnableRateLimitingAttribute()
    {
        // StartCustodyCommand creates a new CustodyRecord row on every call.
        // Without [EnableRateLimiting] an attacker can flood the table with phantom
        // custody records, corrupting the foster-match index and exhausting storage.
        var method = typeof(FostersController)
            .GetMethod("StartCustody", BindingFlags.Public | BindingFlags.Instance);

        method.Should().NotBeNull(
            "FostersController must expose a public StartCustody method");

        var attr = method!.GetCustomAttribute<EnableRateLimitingAttribute>();
        attr.Should().NotBeNull(
            "POST /api/fosters/custody-records/start must carry [EnableRateLimiting] — " +
            "each call inserts a new CustodyRecord; without throttling a compromised " +
            "JWT can flood the table and corrupt the foster-match index");
    }

    [Fact]
    public void FostersController_StartCustody_UsesPublicApiPolicy()
    {
        var method = typeof(FostersController)
            .GetMethod("StartCustody", BindingFlags.Public | BindingFlags.Instance);

        var attr = method!.GetCustomAttribute<EnableRateLimitingAttribute>();
        attr.Should().NotBeNull();
        attr!.PolicyName.Should().Be(
            "public-api",
            "StartCustody must use the 'public-api' policy (30 req/min)");
    }

    [Fact]
    public void FostersController_StartCustody_HasRequestSizeLimitAttribute()
    {
        // Body: GUID + int + optional Note (≤ 500 chars per validator).
        // 4 096 B ceiling stops oversized payloads before FluentValidation.
        var method = typeof(FostersController)
            .GetMethod("StartCustody", BindingFlags.Public | BindingFlags.Instance);

        method.Should().NotBeNull();

        var attr = method!.GetCustomAttribute<RequestSizeLimitAttribute>();
        attr.Should().NotBeNull(
            "POST /api/fosters/custody-records/start must carry [RequestSizeLimit] — " +
            "without an HTTP-layer ceiling an oversized body can reach the parser " +
            "before FluentValidation rejects oversized Note fields");
    }

    // ── PATCH /api/fosters/custody-records/{id}/close ── CloseCustody ─────────

    [Fact]
    public void FostersController_CloseCustody_HasEnableRateLimitingAttribute()
    {
        // CloseCustodyCommand transitions a domain state machine and writes to DB.
        // Without [EnableRateLimiting] an attacker can cycle the state machine
        // repeatedly, generating unbounded DB writes.
        var method = typeof(FostersController)
            .GetMethod("CloseCustody", BindingFlags.Public | BindingFlags.Instance);

        method.Should().NotBeNull(
            "FostersController must expose a public CloseCustody method");

        var attr = method!.GetCustomAttribute<EnableRateLimitingAttribute>();
        attr.Should().NotBeNull(
            "PATCH /api/fosters/custody-records/{id}/close must carry [EnableRateLimiting] — " +
            "each call fires a domain state-machine transition + SaveChangesAsync; " +
            "without throttling a compromised JWT can cycle the state machine " +
            "and generate unbounded DB writes");
    }

    [Fact]
    public void FostersController_CloseCustody_UsesPublicApiPolicy()
    {
        var method = typeof(FostersController)
            .GetMethod("CloseCustody", BindingFlags.Public | BindingFlags.Instance);

        var attr = method!.GetCustomAttribute<EnableRateLimitingAttribute>();
        attr.Should().NotBeNull();
        attr!.PolicyName.Should().Be(
            "public-api",
            "CloseCustody must use the 'public-api' policy (30 req/min)");
    }

    [Fact]
    public void FostersController_CloseCustody_HasRequestSizeLimitAttribute()
    {
        // Body: single outcome string (enum text, short).
        // 2 048 B ceiling stops crafted oversized payloads.
        var method = typeof(FostersController)
            .GetMethod("CloseCustody", BindingFlags.Public | BindingFlags.Instance);

        method.Should().NotBeNull();

        var attr = method!.GetCustomAttribute<RequestSizeLimitAttribute>();
        attr.Should().NotBeNull(
            "PATCH /api/fosters/custody-records/{id}/close must carry [RequestSizeLimit] — " +
            "the body contains only an outcome enum string; a cheap 2 KB ceiling " +
            "stops oversized payloads before they reach the model binder");
    }
}
