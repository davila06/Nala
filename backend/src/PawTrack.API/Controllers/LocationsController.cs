using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PawTrack.Application.Locations.Commands.UpdateUserLocation;
using System.Security.Claims;

namespace PawTrack.API.Controllers;

/// <summary>
/// Manages a user's stored location and geofenced-alert opt-in preference.
/// All endpoints require authentication.
/// </summary>
[ApiController]
[Route("api/me/location")]
[Authorize]
public sealed class LocationsController(ISender sender) : ControllerBase
{
    // ── PUT /api/me/location ──────────────────────────────────────────────────
    /// <summary>
    /// Upserts the authenticated user's last known coordinates and notification preference.
    /// Safe to call on every meaningful position change (the operation is idempotent).
    /// </summary>
    [HttpPut]
    [EnableRateLimiting("location-update")]
    [RequestSizeLimit(4096)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> UpsertLocation(
        [FromBody] UpsertLocationRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        var result = await sender.Send(new UpdateUserLocationCommand(
            userId,
            request.Lat,
            request.Lng,
            request.ReceiveNearbyAlerts,
            request.QuietHoursStart,
            request.QuietHoursEnd),
            cancellationToken);

        return result.IsSuccess
            ? NoContent()
            : UnprocessableEntity(new ProblemDetails
            {
                Title = "Invalid location data",
                Detail = string.Join("; ", result.Errors),
            });
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private bool TryGetUserId(out Guid userId)
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier)
               ?? User.FindFirstValue("sub");

        return Guid.TryParse(raw, out userId);
    }
}

/// <summary>Request body for <c>PUT /api/me/location</c>.</summary>
public sealed record UpsertLocationRequest(
    double Lat,
    double Lng,
    bool ReceiveNearbyAlerts,
    /// <summary>Quiet-hours window start in Costa Rica local time (UTC-6). Null = no quiet window.</summary>
    TimeOnly? QuietHoursStart,
    /// <summary>Quiet-hours window end in Costa Rica local time (UTC-6). Null = no quiet window.</summary>
    TimeOnly? QuietHoursEnd);
