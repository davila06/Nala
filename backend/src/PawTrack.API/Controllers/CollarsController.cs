using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PawTrack.Application.Collars.Commands.RegisterCollar;
using PawTrack.Application.Collars.Queries.GetCollarStatus;
using PawTrack.Domain.Collars;
using System.Security.Claims;

namespace PawTrack.API.Controllers;

[ApiController]
[Route("api/collars")]
[Authorize]
public sealed class CollarsController(ISender sender) : ControllerBase
{
    // ── GET /api/collars/pet/{petId} ─────────────────────────────────────────
    [HttpGet("pet/{petId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStatus(Guid petId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetCollarStatusQuery(petId), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : NotFound();
    }

    // ── POST /api/collars ────────────────────────────────────────────────────
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Register(
        [FromBody] RegisterCollarRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var result = await sender.Send(
            new RegisterCollarCommand(request.PetId, userId, request.Provider, request.ExternalDeviceId),
            cancellationToken);

        if (result.IsFailure)
            return UnprocessableEntity(new ProblemDetails { Detail = string.Join(", ", result.Errors) });

        return Created($"api/collars/pet/{request.PetId}", result.Value);
    }

    private bool TryGetUserId(out Guid userId)
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(raw, out userId);
    }
}

public sealed record RegisterCollarRequest(Guid PetId, CollarProvider Provider, string? ExternalDeviceId);
