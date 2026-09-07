using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PawTrack.Application.Pets.SanitaryIdentity;
using System.Security.Claims;

namespace PawTrack.API.Controllers;

[ApiController]
[Route("api/admin/pets")]
[Authorize(Roles = "Admin")]
public sealed class AdminPetsController(ISender sender) : ControllerBase
{
    [HttpGet("microchip-conflicts")]
    [EnableRateLimiting("public-api")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMicrochipConflicts(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetMicrochipConflictsForAdminQuery(page, pageSize), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Errors);
    }

    [HttpPost("{petId:guid}/microchip-verification/revoke")]
    [EnableRateLimiting("public-api")]
    [RequestSizeLimit(512)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> RevokeMicrochipVerification(
        Guid petId,
        [FromBody] RevokeMicrochipVerificationRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await sender.Send(new RevokeMicrochipVerificationCommand(petId, userId, request.Reason), cancellationToken);
        return result.IsSuccess ? Ok(result.Value)
            : UnprocessableEntity(new ProblemDetails { Detail = string.Join("; ", result.Errors), Status = 422 });
    }

    [HttpPost("{petId:guid}/microchip-conflicts/resolve")]
    [EnableRateLimiting("public-api")]
    [RequestSizeLimit(512)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ResolveMicrochipConflict(
        Guid petId,
        [FromBody] ResolveMicrochipConflictRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await sender.Send(new ResolveMicrochipConflictCommand(
            petId, userId, request.ConfirmedChipId, request.Reason), cancellationToken);
        return result.IsSuccess ? Ok(result.Value)
            : UnprocessableEntity(new ProblemDetails { Detail = string.Join("; ", result.Errors), Status = 422 });
    }

    private bool TryGetUserId(out Guid userId)
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(raw, out userId);
    }
}

public sealed record RevokeMicrochipVerificationRequest(string Reason);
public sealed record ResolveMicrochipConflictRequest(string ConfirmedChipId, string Reason);