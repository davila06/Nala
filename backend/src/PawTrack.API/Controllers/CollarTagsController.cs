using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PawTrack.Application.Collars.Commands.ActivateCollarTag;
using PawTrack.Application.Collars.Commands.DeactivateCollarTag;
using PawTrack.Application.Collars.Commands.IngestCollarLocation;
using PawTrack.Application.Collars.Queries.CheckCollarSerial;
using System.Security.Claims;

namespace PawTrack.API.Controllers;

[ApiController]
[Route("api/collars")]
public sealed class CollarTagsController(ISender sender) : ControllerBase
{
    // ── GET /api/collars/tag/{serial} — availability check ───────────────────
    [HttpGet("tag/{serial}")]
    [Authorize]
    [EnableRateLimiting("collar-serial-check")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CheckSerial(string serial, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CheckCollarSerialQuery(serial), cancellationToken);
        if (result.IsFailure) return NotFound(new ProblemDetails { Detail = string.Join(", ", result.Errors) });
        return Ok(result.Value);
    }

    // ── POST /api/collars/tag/{serial}/activate ───────────────────────────────
    [HttpPost("tag/{serial}/activate")]
    [Authorize]
    [EnableRateLimiting("collar-serial-check")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Activate(string serial, [FromBody] ActivateCollarTagRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var result = await sender.Send(new ActivateCollarTagCommand(serial, request.PetId, userId), cancellationToken);
        if (result.IsFailure)
            return UnprocessableEntity(new ProblemDetails { Detail = string.Join(", ", result.Errors) });

        return Ok(result.Value);
    }

    // ── DELETE /api/collars/tag/{serial}/deactivate ───────────────────────────
    [HttpDelete("tag/{serial}/deactivate")]
    [Authorize]
    [EnableRateLimiting("collar-serial-check")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Deactivate(string serial, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var result = await sender.Send(new DeactivateCollarTagCommand(serial, userId), cancellationToken);
        if (result.IsFailure)
            return result.Errors.Contains("Access denied.")
                ? Forbid()
                : UnprocessableEntity(new ProblemDetails { Detail = string.Join(", ", result.Errors) });

        return NoContent();
    }

    // ── POST /api/collars/ingest — device push (X-Collar-Key auth) ───────────
    [HttpPost("ingest")]
    [AllowAnonymous] // auth handled by CollarDeviceKeyMiddleware
    [EnableRateLimiting("location-update")] // caps device-key stuffing/spam per IP
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Ingest([FromBody] IngestLocationRequest request, CancellationToken cancellationToken)
    {
        var collarIdClaim = User.FindFirstValue("CollarId");
        if (string.IsNullOrEmpty(collarIdClaim) || !Guid.TryParse(collarIdClaim, out var collarId))
            return Unauthorized(new ProblemDetails { Detail = "Missing or invalid device key.", Status = 401 });

        var result = await sender.Send(new IngestCollarLocationCommand(
            collarId, request.Serial, request.Lat, request.Lng,
            request.BatteryPercent, request.Timestamp, request.AccuracyMeters), cancellationToken);

        if (result.IsFailure)
            return UnprocessableEntity(new ProblemDetails { Detail = string.Join(", ", result.Errors) });

        return NoContent();
    }

    private bool TryGetUserId(out Guid userId)
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(raw, out userId);
    }
}

public sealed record ActivateCollarTagRequest(Guid PetId);

public sealed record IngestLocationRequest(
    string Serial,
    double Lat,
    double Lng,
    int? BatteryPercent,
    DateTimeOffset Timestamp,
    int? AccuracyMeters);
