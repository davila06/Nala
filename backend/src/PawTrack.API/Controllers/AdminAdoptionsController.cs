using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PawTrack.Application.Adoptions;

namespace PawTrack.API.Controllers;

[ApiController]
[Route("api/admin/adoptions")]
[Authorize(Roles = "Admin")]
public sealed class AdminAdoptionsController(ISender sender) : ControllerBase
{
    [HttpGet("stats")]
    [EnableRateLimiting("public-api")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStats(CancellationToken ct)
    {
        var result = await sender.Send(new GetAdoptionAdminStatsQuery(), ct);
        return Ok(result.Value);
    }

    [HttpGet("animals")]
    [EnableRateLimiting("public-api")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAnimals(
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        pageSize = Math.Clamp(pageSize, 1, 50);
        var result = await sender.Send(
            new GetAllAdoptableAnimalsAdminQuery(status, Math.Max(1, page), pageSize), ct);
        return Ok(result.Value);
    }

    [HttpPatch("animals/{id:guid}/moderate")]
    [EnableRateLimiting("public-api")]
    [RequestSizeLimit(256)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Moderate(
        Guid id,
        [FromBody] ModerateAnimalRequest request,
        CancellationToken ct)
    {
        var result = await sender.Send(new AdminModerateAnimalCommand(id, request.Action), ct);
        if (result.IsFailure)
            return BadRequest(new ProblemDetails { Detail = string.Join("; ", result.Errors), Status = 400 });
        return NoContent();
    }
}

public sealed record ModerateAnimalRequest(string Action);
