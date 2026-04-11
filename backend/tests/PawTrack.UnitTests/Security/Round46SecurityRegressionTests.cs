using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using PawTrack.API.Controllers;
using System.Reflection;

namespace PawTrack.UnitTests.Security;

/// <summary>
/// Round-46 security regression tests.
///
/// Gap: <c>POST /api/sightings/visual-match</c> accepts a multipart photo upload
/// but does NOT carry <c>[RequestSizeLimit]</c>:
///
///   <code>
///   [HttpPost("visual-match")]
///   [AllowAnonymous]
///   [EnableRateLimiting("sightings")]
///   // ← [RequestSizeLimit] missing
///   public async Task&lt;IActionResult&gt; VisualMatch([FromForm] VisualMatchRequest request, ...)
///   </code>
///
/// The handler contains an application-level check:
///
///   <code>
///   if (request.Photo.Length > 5 * 1024 * 1024)  // 5 MB
///       return UnprocessableEntity(...);
///   </code>
///
/// Without <c>[RequestSizeLimit(5_242_880)]</c>, Kestrel's global 1 MB cap fires
/// first and returns 413 before the handler runs — the in-code 5 MB check is
/// dead code.  Meanwhile, <c>POST /api/sightings</c> and <c>POST /api/found-pets</c>
/// both carry <c>[RequestSizeLimit(5_242_880)]</c> for the same use-case,
/// making this endpoint inconsistent.
///
/// Anonymous access + no size limit = an attacker can send 1 MB photo payloads
/// continuously, saturating the sightings rate limiter at the byte level without
/// body processing.
///
/// Fix:
///   Add <c>[RequestSizeLimit(5_242_880)]</c> to the <c>VisualMatch</c> action.
/// </summary>
public sealed class Round46SecurityRegressionTests
{
    private const long FiveMegabytes = 5 * 1024 * 1024; // 5_242_880

    [Fact]
    public void SightingsController_VisualMatch_HasRequestSizeLimitAttribute()
    {
        var method = typeof(SightingsController)
            .GetMethod("VisualMatch", BindingFlags.Public | BindingFlags.Instance)!;

        var attr = method.GetCustomAttribute<RequestSizeLimitAttribute>();

        attr.Should().NotBeNull(
            "POST /api/sightings/visual-match accepts a multipart photo upload " +
            "and requires [RequestSizeLimit(5_242_880)] to raise Kestrel's global 1 MB cap " +
            "to 5 MB — matching the in-handler check and the behaviour of the related " +
            "POST /api/sightings and POST /api/found-pets endpoints");
    }

    [Fact]
    public void SightingsController_VisualMatch_RequestSizeLimitIs5Megabytes()
    {
        var method = typeof(SightingsController)
            .GetMethod("VisualMatch", BindingFlags.Public | BindingFlags.Instance)!;

        var attr = method.GetCustomAttribute<RequestSizeLimitAttribute>();

        attr.Should().NotBeNull();

        // RequestSizeLimitAttribute stores the limit in a private field _bytes.
        // We read it via reflection since the attribute has no public property.
        var bytesField = typeof(RequestSizeLimitAttribute)
            .GetField("_bytes", BindingFlags.NonPublic | BindingFlags.Instance);
        bytesField.Should().NotBeNull("RequestSizeLimitAttribute must have a _bytes backing field");

        var bytesValue = (long)bytesField!.GetValue(attr)!;
        bytesValue.Should().Be(FiveMegabytes,
            "the request size limit must match the 5 MB in-handler guard " +
            "so the guard is reachable and the limits are consistent");
    }
}
