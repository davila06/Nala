using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PawTrack.Application.LostPets.Commands.ActivateSearchCoordination;
using PawTrack.Application.LostPets.Queries.GetSearchZones;
using PawTrack.Application.LostPets.Queries.IsSearchParticipant;
using System.Security.Claims;

namespace PawTrack.API.Controllers;

/// <summary>
/// REST endpoints for coordinated field search (Mejora H).
/// Zone state changes (claim/clear/release) go through the SignalR hub at
/// <c>/hubs/search-coordination</c> for real-time broadcast.
/// </summary>
[ApiController]
[Route("api/search-coordination")]
[Authorize]
public sealed class SearchCoordinationController(ISender sender) : ControllerBase
{
    // ── POST /api/search-coordination/{lostEventId}/activate ─────────────────
    /// <summary>
    /// Activates coordinated search mode. Generates a 7×7 grid of 300 m search zones
    /// centred on the last-seen location. Idempotent — safe to call multiple times.
    /// Only the owner of the lost-pet report may activate.
    /// </summary>
    [HttpPost("{lostEventId:guid}/activate")]
    [EnableRateLimiting("public-api")] // 30/min — prevents 49-zone grid spam (idempotent but still writes DB)
    [ProducesResponseType(typeof(ActivateSearchCoordinationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Activate(Guid lostEventId, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        var result = await sender.Send(
            new ActivateSearchCoordinationCommand(lostEventId, userId), cancellationToken);

        if (result.IsFailure)
        {
            if (result.Errors.Contains("Lost pet report not found."))
                return NotFound(new ProblemDetails { Title = "Lost pet report not found", Status = 404 });

            if (result.Errors.Contains("Access denied."))
                return Forbid();

            return UnprocessableEntity(new ProblemDetails
            {
                Title = "Cannot activate search coordination",
                Status = 422,
                Extensions = { ["errors"] = result.Errors },
            });
        }

        return Ok(new ActivateSearchCoordinationResponse(lostEventId, result.Value!));
    }

    // ── GET /api/search-coordination/{lostEventId}/zones ─────────────────────
    /// <summary>Returns all search zones for the given lost-pet event, ordered by label.</summary>
    [HttpGet("{lostEventId:guid}/zones")]
    [EnableRateLimiting("public-api")] // 30/min — prevents event-ID enumeration without throttling
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetZones(Guid lostEventId, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        // BOLA gate — mirrors the participant check applied to every SignalR hub method.
        // Any authenticated user can discover active lostEventIds via the public map;
        // without this guard they could poll the zone grid to track volunteer movements.
        var participation = await sender.Send(
            new IsSearchParticipantQuery(lostEventId, userId), cancellationToken);

        if (participation.IsFailure || !participation.Value)
            return Forbid();

        var result = await sender.Send(new GetSearchZonesQuery(lostEventId), cancellationToken);

        if (result.IsFailure)
            return NotFound(new ProblemDetails { Title = "Lost pet report not found", Status = 404 });

        return Ok(result.Value);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    private bool TryGetUserId(out Guid userId)
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? User.FindFirstValue("sub");
        return Guid.TryParse(claim, out userId);
    }
}

public sealed record ActivateSearchCoordinationResponse(Guid LostPetEventId, int ZoneCount);
