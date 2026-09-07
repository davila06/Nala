using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PawTrack.Application.Regulatory.Queries;

namespace PawTrack.API.Controllers;

[ApiController]
[Route("api/nala")]
[Authorize(Roles = "Admin,Nala")]
public sealed class NalaController(ISender sender, IConfiguration configuration) : ControllerBase
{
    [HttpGet("overview")]
    [EnableRateLimiting("public-api")]
    public async Task<IActionResult> GetOverview(
        [FromQuery] DateOnly periodStart,
        [FromQuery] DateOnly periodEnd,
        CancellationToken cancellationToken)
    {
        if (!configuration.GetValue("Features:NalaDashboardEnabled", true)) return NotFound();
        var result = await sender.Send(new GetNalaOverviewQuery(periodStart, periodEnd), cancellationToken);
        if (result.IsSuccess)
        {
            var tag = $"{result.Value!.GeneratedAt.Ticks:x}";
            Response.Headers.ETag = $"\"{tag}\"";
            if (Request.Headers.IfNoneMatch == $"\"{tag}\"") return StatusCode(StatusCodes.Status304NotModified);
            Response.Headers.CacheControl = "private, max-age=60";
        }
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Errors);
    }

    [HttpGet("map-layers")]
    [EnableRateLimiting("public-api")]
    public async Task<IActionResult> GetMapLayers(
        [FromQuery] double south,
        [FromQuery] double north,
        [FromQuery] double west,
        [FromQuery] double east,
        CancellationToken cancellationToken)
    {
        if (!configuration.GetValue("Features:NalaDashboardEnabled", true)) return NotFound();
        var result = await sender.Send(new GetNalaMapLayersQuery(south, north, west, east), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Errors);
    }

    [HttpGet("trends")]
    [EnableRateLimiting("public-api")]
    public async Task<IActionResult> GetTrends(
        [FromQuery] DateOnly periodStart,
        [FromQuery] DateOnly periodEnd,
        [FromQuery] string? canton,
        CancellationToken cancellationToken)
    {
        if (!configuration.GetValue("Features:NalaDashboardEnabled", true)) return NotFound();
        var result = await sender.Send(new GetNalaTrendsQuery(periodStart, periodEnd, canton), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Errors);
    }

    [HttpGet("cantons")]
    [EnableRateLimiting("public-api")]
    public async Task<IActionResult> GetCantons([FromQuery] DateOnly periodStart, [FromQuery] DateOnly periodEnd, CancellationToken cancellationToken)
    {
        if (!configuration.GetValue("Features:NalaDashboardEnabled", true)) return NotFound();
        var result = await sender.Send(new GetNalaCantonSummaryQuery(periodStart, periodEnd), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Errors);
    }

    [HttpGet("institutions")]
    [EnableRateLimiting("public-api")]
    public async Task<IActionResult> GetInstitutions([FromQuery] DateOnly periodStart, [FromQuery] DateOnly periodEnd, CancellationToken cancellationToken)
    {
        if (!configuration.GetValue("Features:NalaDashboardEnabled", true)) return NotFound();
        var result = await sender.Send(new GetNalaInstitutionPerformanceQuery(periodStart, periodEnd), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Errors);
    }
}
