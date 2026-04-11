using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PawTrack.Application.Sightings.Queries.GetMovementPrediction;
using PawTrack.Application.Sightings.Queries.GetPublicMapEvents;

namespace PawTrack.API.Controllers;

/// <summary>
/// Public endpoints for the map — no authentication required.
/// </summary>
[ApiController]
[Route("api/public")]
[EnableRateLimiting("public-api")] // 30 req/min per IP — protects DB-aggregation queries
public sealed class PublicMapController(ISender sender) : ControllerBase
{
    // Maximum degree span allowed per map-tile request.
    // 5° ≈ 555 km at the equator — larger than all of Costa Rica (≈ 460 km N-S).
    // Prevents a single anonymous request from scanning the full sightings + lost-pet tables.
    private const double MaxDegreeSpan = 5.0;

    // ── GET /api/public/map?north=&south=&east=&west= ─────────────────────────
    [HttpGet("map")]
    [ResponseCache(Duration = 30, Location = ResponseCacheLocation.Any,
        VaryByQueryKeys = ["north", "south", "east", "west"])]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> GetMapEvents(
        [FromQuery] double north,
        [FromQuery] double south,
        [FromQuery] double east,
        [FromQuery] double west,
        CancellationToken cancellationToken)
    {
        // ── GPS-range guard —————————————————————————————————————————————————
        // Rejects out-of-range coordinates that would pass the ordering check but
        // would still be meaningless (or crafted) values (e.g. north=99999).
        if (north > 90 || north < -90 || south > 90 || south < -90 ||
            east > 180 || east < -180 || west > 180 || west < -180)
        {
            return UnprocessableEntity(new ProblemDetails
            {
                Title = "Invalid bounding box",
                Status = 422,
                Detail = "Coordinates must be in valid WGS-84 ranges (lat: ±90, lng: ±180).",
            });
        }

        if (north < south || east < west)
        {
            return UnprocessableEntity(new ProblemDetails
            {
                Title = "Invalid bounding box",
                Status = 422,
                Detail = "north must be >= south and east must be >= west.",
            });
        }

        // ── Max-area guard ——————————————————————————————————————————————————
        // A full-globe bbox (90 - -90 = 180°) forces a complete table scan on two
        // large tables in parallel. Cap the span to limit DB scan cost.
        if ((north - south) > MaxDegreeSpan || (east - west) > MaxDegreeSpan)
        {
            return UnprocessableEntity(new ProblemDetails
            {
                Title = "Bounding box too large",
                Status = 422,
                Detail = $"Each axis of the bounding box must span ≤ {MaxDegreeSpan}°.",
            });
        }

        var result = await sender.Send(
            new GetPublicMapEventsQuery(north, south, east, west), cancellationToken);

        if (result.IsFailure)
        {
            return UnprocessableEntity(new ProblemDetails
            {
                Title = "Validation error",
                Status = 422,
                Extensions = { ["errors"] = result.Errors },
            });
        }

        return Ok(result.Value);
    }

    // ── GET /api/public/movement/{lostPetEventId} ─────────────────────────────
    /// <summary>
    /// Returns a predictive movement projection for a lost-pet event.
    /// Computed from all sightings linked to that event — no new tables required.
    /// Always returns 200 OK; check <c>hasEnoughData</c> in the response body.
    /// </summary>
    [HttpGet("movement/{lostPetEventId:guid}")]
    [ResponseCache(Duration = 60, Location = ResponseCacheLocation.Any)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMovementPrediction(
        Guid lostPetEventId,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new GetMovementPredictionQuery(lostPetEventId), cancellationToken);

        // The handler never produces a Failure result; IsFailure guard is defensive.
        if (result.IsFailure)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Error al calcular predicción de movimiento",
                Status = 500,
            });
        }

        return Ok(result.Value);
    }
}
