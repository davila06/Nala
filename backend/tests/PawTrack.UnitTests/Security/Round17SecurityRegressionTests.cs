using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using PawTrack.API.Controllers;

namespace PawTrack.UnitTests.Security;

/// <summary>
/// Round-17 security regression tests.
///
/// Gap: The two admin endpoints in <see cref="AlliesController"/> rely solely on a
/// manual <c>TryGetRole(out var role) || role != UserRole.Admin</c> check inside
/// the action body.  They are NOT decorated with
/// <c>[Authorize(Roles = "Admin")]</c> at the framework level.
///
/// Problem:
///   1. Defense-in-depth is weaker — if the class-level <c>[Authorize]</c> is ever
///      removed (e.g. during a refactor), the admin check becomes the only guard,
///      but because it returns <c>Forbid()</c> instead of <c>Unauthorized()</c>,
///      the response code leaks that the endpoint exists and requires a higher role.
///   2. The manual <c>TryGetRole</c> approach is not consistent with the rest of
///      the API (ClinicsController uses <c>[Authorize(Roles = "Admin")]</c>).
///   3. Framework-level role authorization is evaluated before the action body
///      executes; manual checks are not — they can be accidentally bypassed during
///      refactors that restructure early-return control flow.
/// </summary>
public sealed class Round17SecurityRegressionTests
{
    // ── AlliesController.GetPendingApplications ────────────────────────────────

    [Fact]
    public void AlliesController_GetPendingApplications_HasAuthorizeAdminAttribute()
    {
        var method = typeof(AlliesController)
            .GetMethod(nameof(AlliesController.GetPendingApplications));

        method.Should().NotBeNull();

        var authorizeAttr = method!
            .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: false)
            .Cast<AuthorizeAttribute>()
            .FirstOrDefault();

        authorizeAttr.Should().NotBeNull(
            "GetPendingApplications must carry [Authorize(Roles=\"Admin\")] " +
            "so the framework rejects non-admin requests before the action body runs");

        authorizeAttr!.Roles.Should().Be("Admin",
            "only Admin role users may list pending ally applications");
    }

    // ── AlliesController.ReviewApplication ────────────────────────────────────

    [Fact]
    public void AlliesController_ReviewApplication_HasAuthorizeAdminAttribute()
    {
        var method = typeof(AlliesController)
            .GetMethod(nameof(AlliesController.ReviewApplication));

        method.Should().NotBeNull();

        var authorizeAttr = method!
            .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: false)
            .Cast<AuthorizeAttribute>()
            .FirstOrDefault();

        authorizeAttr.Should().NotBeNull(
            "ReviewApplication must carry [Authorize(Roles=\"Admin\")] " +
            "so the framework rejects non-admin requests before the action body runs");

        authorizeAttr!.Roles.Should().Be("Admin",
            "only Admin role users may approve or reject ally applications");
    }
}
