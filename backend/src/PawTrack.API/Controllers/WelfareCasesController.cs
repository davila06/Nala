using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PawTrack.Application.AnimalWelfare.Queries;
using System.Security.Claims;

namespace PawTrack.API.Controllers;

[ApiController]
[Route("api/welfare-cases")]
[Authorize(Roles = "Admin,Municipality,Clinic,Ally,Shelter")]
public sealed class WelfareCasesController(ISender sender) : ControllerBase
{
    [HttpGet("assigned")]
    [EnableRateLimiting("public-api")]
    public async Task<IActionResult> GetAssigned(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await sender.Send(new GetAssignedWelfareCasesQuery(userId, page, pageSize), cancellationToken);
        return Ok(result.Value);
    }

    private bool TryGetUserId(out Guid userId)
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(raw, out userId);
    }
}
