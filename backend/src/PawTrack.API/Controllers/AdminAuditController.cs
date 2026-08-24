using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PawTrack.Application.Audit;

namespace PawTrack.API.Controllers;

[ApiController]
[Route("api/admin/audit")]
[Authorize(Roles = "Admin")]
public sealed class AdminAuditController(ISender sender) : ControllerBase
{
    [HttpGet]
    [EnableRateLimiting("public-api")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLog(
        [FromQuery] string? entityType,
        [FromQuery] string? entityId,
        [FromQuery] int take = 100,
        CancellationToken ct = default)
    {
        var result = await sender.Send(new GetAuditLogQuery(entityType, entityId, take), ct);
        return Ok(result.Value);
    }
}
