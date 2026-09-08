using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PawTrack.Application.ServiceProviders;
using System.Security.Claims;

namespace PawTrack.API.Controllers;

[ApiController]
[Route("api/support/provider-incidents")]
[Authorize(Roles = "Admin,Support")]
[EnableRateLimiting("public-api")]
public sealed class SupportProviderIncidentsController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetIncidents(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var result = await sender.Send(new GetProviderIncidentsForAdminQuery(page, pageSize), ct);
        return result.IsSuccess ? Ok(result.Value) : Problem(detail: string.Join("; ", result.Errors));
    }

    [HttpPut("{incidentId:guid}/investigation")]
    public async Task<IActionResult> StartInvestigation(
        Guid incidentId,
        CancellationToken ct)
    {
        if (!TryGetActorId(out var actorId)) return Unauthorized();
        var result = await sender.Send(new StartProviderIncidentInvestigationCommand(actorId, incidentId, actorId), ct);
        return result.IsSuccess ? Ok(result.Value) : UnprocessableEntity(new ProblemDetails { Detail = string.Join("; ", result.Errors), Status = 422 });
    }

    [HttpPut("{incidentId:guid}/resolve")]
    public async Task<IActionResult> Resolve(
        Guid incidentId,
        [FromBody] ResolveSupportIncidentRequest request,
        CancellationToken ct)
    {
        if (!TryGetActorId(out var actorId)) return Unauthorized();
        var result = await sender.Send(new ResolveProviderIncidentCommand(actorId, incidentId, request.Resolution), ct);
        return result.IsSuccess ? Ok(result.Value) : UnprocessableEntity(new ProblemDetails { Detail = string.Join("; ", result.Errors), Status = 422 });
    }

    [HttpPut("{incidentId:guid}/close")]
    public async Task<IActionResult> Close(Guid incidentId, CancellationToken ct)
    {
        if (!TryGetActorId(out var actorId)) return Unauthorized();
        var result = await sender.Send(new CloseProviderIncidentCommand(actorId, incidentId), ct);
        return result.IsSuccess ? Ok(result.Value) : UnprocessableEntity(new ProblemDetails { Detail = string.Join("; ", result.Errors), Status = 422 });
    }

    private bool TryGetActorId(out Guid actorId)
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(claim, out actorId);
    }
}

public sealed record ResolveSupportIncidentRequest(string Resolution);
