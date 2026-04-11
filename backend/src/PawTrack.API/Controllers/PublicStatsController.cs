using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PawTrack.Application.LostPets.Queries.GetRecoveryOverview;
using PawTrack.Application.LostPets.Queries.GetRecoveryRates;

namespace PawTrack.API.Controllers;

[ApiController]
[Route("api/public/stats")]
[EnableRateLimiting("public-api")] // 30 req/min per IP — protects DB-aggregation queries
public sealed class PublicStatsController(ISender sender) : ControllerBase
{
    [HttpGet("recovery-rates")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRecoveryRates(
        [FromQuery] string? species,
        [FromQuery] string? breed,
        [FromQuery] string? canton,
        CancellationToken cancellationToken)
    {
        var response = await sender.Send(
            new GetRecoveryRatesQuery(species, breed, canton),
            cancellationToken);

        return Ok(response);
    }

    [HttpGet("recovery-overview")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRecoveryOverview(CancellationToken cancellationToken)
    {
        var response = await sender.Send(new GetRecoveryOverviewQuery(), cancellationToken);
        return Ok(response);
    }
}
