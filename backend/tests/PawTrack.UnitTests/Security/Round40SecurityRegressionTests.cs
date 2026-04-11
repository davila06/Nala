using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using PawTrack.API.Controllers;
using System.Reflection;

namespace PawTrack.UnitTests.Security;

/// <summary>
/// Round-40 security regression tests.
///
/// Gap: Two write endpoints that transition domain state carry NO <c>[RequestSizeLimit]</c>:
///
/// ── <c>PUT /api/lost-pets/{id}/status</c> — <c>UpdateStatus</c> ──────────────
///   Body: <c>UpdateLostPetStatusRequest(NewStatus, ConfirmedSightingId?)</c>.
///   Realistic maximum: status enum string (≤ 20 chars) + optional GUID string
///   ≈ 80 bytes. Without a ceiling the ASP.NET runtime buffers and deserializes
///   up to 1 MB before the handler fires. The handler triggers the lost-pet state
///   machine + may dispatch domain events → DB writes under inflated load.
///
/// ── <c>POST /api/allies/admin/applications/{userId}/review</c> — <c>ReviewApplication</c>
///   Body: <c>ReviewAllyApplicationRequest(Approve: bool)</c>.
///   Realistic maximum: ≈ 20 bytes (`{"approve":true}`).
///   Without a ceiling, a 1 MB body triggers full deserialization + an Admin-scoped
///   DB write on every call. Even with the "public-api" rate limiter (30/min),
///   attacker-controlled bodies of 30 × 1 MB = 30 MB/min of body data keep
///   the JSON deserializer busy before any validation fires.
///
/// Fix:
///   <c>[RequestSizeLimit(512)]</c>  on <c>LostPetsController.UpdateStatus</c>
///   <c>[RequestSizeLimit(128)]</c>  on <c>AlliesController.ReviewApplication</c>
/// </summary>
public sealed class Round40SecurityRegressionTests
{
    // ── LostPetsController.UpdateStatus ──────────────────────────────────────

    [Fact]
    public void LostPetsController_UpdateStatus_HasRequestSizeLimitAttribute()
    {
        // Arrange
        var method = typeof(LostPetsController)
            .GetMethod("UpdateStatus", BindingFlags.Public | BindingFlags.Instance);

        // Act
        var attr = method?.GetCustomAttribute<RequestSizeLimitAttribute>();

        // Assert
        attr.Should().NotBeNull(
            "PUT /api/lost-pets/{id}/status accepts [FromBody] with a status enum + optional GUID; " +
            "a per-action [RequestSizeLimit] prevents 1 MB bodies from reaching the " +
            "lost-pet state machine before FluentValidation fires.");
    }

    // ── AlliesController.ReviewApplication ───────────────────────────────────

    [Fact]
    public void AlliesController_ReviewApplication_HasRequestSizeLimitAttribute()
    {
        // Arrange
        var method = typeof(AlliesController)
            .GetMethod("ReviewApplication", BindingFlags.Public | BindingFlags.Instance);

        // Act
        var attr = method?.GetCustomAttribute<RequestSizeLimitAttribute>();

        // Assert
        attr.Should().NotBeNull(
            "POST /api/allies/admin/applications/{userId}/review accepts [FromBody] " +
            "with a single bool; without [RequestSizeLimit] a 1 MB body is fully " +
            "deserialized before the Admin-scoped DB write fires.");
    }
}
