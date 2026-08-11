using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PawTrack.Application.Locations;
using System.Security.Claims;

namespace PawTrack.API.Controllers;

[ApiController]
[Route("api/neighbor")]
public sealed class NeighborNetworkController(ISender sender) : ControllerBase
{
    // ── GET /api/neighbor/status ──────────────────────────────────────────────
    [HttpGet("status")]
    [Authorize]
    [EnableRateLimiting("public-api")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStatus(CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await sender.Send(new GetNeighborStatusQuery(userId), ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Errors);
    }

    // ── POST /api/neighbor/enroll ─────────────────────────────────────────────
    [HttpPost("enroll")]
    [Authorize]
    [EnableRateLimiting("public-api")]
    [RequestSizeLimit(512)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Enroll(
        [FromBody] EnrollNeighborRequest request,
        CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await sender.Send(
            new EnrollNeighborAlertCommand(userId, request.Phone, request.RadiusMeters), ct);
        if (result.IsFailure)
            return UnprocessableEntity(new ProblemDetails { Detail = string.Join("; ", result.Errors), Status = 422 });
        return Ok(result.Value);
    }

    // ── PUT /api/neighbor/settings ────────────────────────────────────────────
    [HttpPut("settings")]
    [Authorize]
    [EnableRateLimiting("public-api")]
    [RequestSizeLimit(256)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> UpdateSettings(
        [FromBody] UpdateNeighborSettingsRequest request,
        CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await sender.Send(
            new UpdateNeighborSettingsCommand(userId, request.RadiusMeters, request.IsActive), ct);
        if (result.IsFailure)
            return UnprocessableEntity(new ProblemDetails { Detail = string.Join("; ", result.Errors), Status = 422 });
        return NoContent();
    }

    // ── GET /api/public/neighbor-count?lat=&lng=&radius= ─────────────────────
    [HttpGet("/api/public/neighbor-count")]
    [AllowAnonymous]
    [EnableRateLimiting("public-api")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCount(
        [FromQuery] double lat,
        [FromQuery] double lng,
        [FromQuery] int radius = 500,
        CancellationToken ct = default)
    {
        var result = await sender.Send(
            new GetNeighborCountInAreaQuery(lat, lng, Math.Clamp(radius, 100, 2000)), ct);
        return result.IsSuccess ? Ok(new { count = result.Value }) : Ok(new { count = 0 });
    }

    private bool TryGetUserId(out Guid userId)
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? User.FindFirstValue("sub");
        return Guid.TryParse(claim, out userId);
    }
}

public sealed record EnrollNeighborRequest(string Phone, int RadiusMeters = 500);
public sealed record UpdateNeighborSettingsRequest(int RadiusMeters, bool IsActive);
