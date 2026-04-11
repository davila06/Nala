using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PawTrack.Application.Broadcast.Commands.BroadcastLostPet;
using PawTrack.Application.Broadcast.Queries.GetBroadcastStatus;
using System.Security.Claims;

namespace PawTrack.API.Controllers;

/// <summary>
/// Multi-channel broadcast for lost-pet reports.
/// All endpoints are owner-gated: only the owner of the lost-pet event may
/// trigger or inspect a broadcast.
/// </summary>
[ApiController]
[Route("api/broadcast")]
[Authorize]
public sealed class BroadcastController(ISender sender) : ControllerBase
{
    // ── POST /api/broadcast/lost-pets/{lostEventId} ────────────────────────────
    /// <summary>
    /// Triggers a multi-channel broadcast (Email, WhatsApp, Telegram, Facebook)
    /// for the given active lost-pet report. Returns one result per channel.
    /// Can be called multiple times (re-broadcast / retry).
    /// </summary>
    [HttpPost("lost-pets/{lostEventId:guid}")]
    [EnableRateLimiting("broadcast")] // 3 broadcasts/10 min — prevents external API spam
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> TriggerBroadcast(
        Guid lostEventId,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        var result = await sender.Send(
            new BroadcastLostPetCommand(lostEventId, userId),
            cancellationToken);

        if (result.IsFailure)
        {
            return result.Errors.Contains("Access denied.") ? Forbid() :
                   result.Errors.Contains("Lost pet report not found.") || result.Errors.Contains("Pet not found.") || result.Errors.Contains("Owner not found.")
                       ? NotFound(new ProblemDetails { Title = "Not found", Status = 404, Detail = result.Errors[0] })
                       : UnprocessableEntity(new ProblemDetails
                       {
                           Title = "Broadcast error",
                           Status = 422,
                           Extensions = { ["errors"] = result.Errors },
                       });
        }

        return Ok(result.Value);
    }

    // ── GET /api/broadcast/lost-pets/{lostEventId} ─────────────────────────────
    /// <summary>
    /// Returns the broadcast status for a lost-pet event:
    /// all channel attempts with their individual statuses and tracking click counts.
    /// </summary>
    [HttpGet("lost-pets/{lostEventId:guid}")]
    [EnableRateLimiting("public-api")] // 30/min — prevents high-frequency status polling
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBroadcastStatus(
        Guid lostEventId,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        var result = await sender.Send(
            new GetBroadcastStatusQuery(lostEventId, userId),
            cancellationToken);

        if (result.IsFailure)
        {
            return result.Errors.Contains("Access denied.") ? Forbid() :
                   NotFound(new ProblemDetails { Title = "Not found", Status = 404, Detail = result.Errors[0] });
        }

        return Ok(result.Value);
    }

    // ── Private helpers ────────────────────────────────────────────────────────

    private bool TryGetUserId(out Guid userId)
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(raw, out userId);
    }
}
