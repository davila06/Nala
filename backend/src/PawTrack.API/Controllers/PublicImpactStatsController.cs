using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PawTrack.Application.Regulatory.Queries;

namespace PawTrack.API.Controllers;

[ApiController]
[Route("api/public/impact-stats")]
public sealed class PublicImpactStatsController(ISender sender) : ControllerBase
{
    [HttpGet]
    [EnableRateLimiting("public-api")]
    public async Task<IActionResult> Get(
        [FromQuery] DateOnly periodStart,
        [FromQuery] DateOnly periodEnd,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetPublicImpactStatsQuery(periodStart, periodEnd), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Errors);
    }
}
