using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PawTrack.Application.Medical.ClinicAccess;
using System.Security.Claims;

namespace PawTrack.API.Controllers;

/// <summary>
/// Owner-side management of clinic access grants for a pet's expediente.
/// All routes require the authenticated user to be the pet's owner.
/// </summary>
[ApiController]
[Route("api/pets/{petId:guid}/clinic-access")]
[Authorize]
public sealed class PetClinicAccessController(ISender sender) : ControllerBase
{
    // ── GET /api/pets/{petId}/clinic-access ───────────────────────────────────
    /// <summary>List all non-revoked grants for this pet (active + pending).</summary>
    [HttpGet]
    [EnableRateLimiting("public-api")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetGrants(Guid petId, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await sender.Send(new GetPetClinicGrantsQuery(petId, userId), ct);
        return result.IsSuccess ? Ok(result.Value) : Forbid();
    }

    // ── POST /api/pets/{petId}/clinic-access/code ─────────────────────────────
    /// <summary>
    /// Owner generates an 8-char code to hand to their clinic.
    /// The clinic enters the code to activate permanent access.
    /// </summary>
    [HttpPost("code")]
    [EnableRateLimiting("public-api")]
    [RequestSizeLimit(256)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> GenerateCode(
        Guid petId,
        [FromBody] GenerateOwnerCodeRequest request,
        CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await sender.Send(
            new OwnerGenerateAccessCodeCommand(petId, userId, request.ClinicId), ct);

        if (result.IsFailure)
            return UnprocessableEntity(new ProblemDetails { Detail = result.Errors.FirstOrDefault(), Status = 422 });

        return Created(string.Empty, result.Value);
    }

    // ── POST /api/pets/{petId}/clinic-access/accept ───────────────────────────
    /// <summary>
    /// Owner enters the code their clinic generated to activate the grant.
    /// </summary>
    [HttpPost("accept")]
    [EnableRateLimiting("public-api")]
    [RequestSizeLimit(256)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> AcceptClinicCode(
        Guid petId,
        [FromBody] AcceptCodeRequest request,
        CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await sender.Send(
            new OwnerAcceptClinicCodeCommand(petId, userId, request.Code), ct);

        if (result.IsFailure)
            return UnprocessableEntity(new ProblemDetails { Detail = result.Errors.FirstOrDefault(), Status = 422 });

        return Ok(result.Value);
    }

    // ── DELETE /api/pets/{petId}/clinic-access/{clinicId} ─────────────────────
    /// <summary>Owner revokes a clinic's access to this pet's expediente.</summary>
    [HttpDelete("{clinicId:guid}")]
    [EnableRateLimiting("public-api")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> RevokeAccess(
        Guid petId, Guid clinicId, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await sender.Send(
            new RevokeClinicAccessGrantCommand(petId, userId, clinicId), ct);

        if (result.IsFailure)
            return UnprocessableEntity(new ProblemDetails { Detail = result.Errors.FirstOrDefault(), Status = 422 });

        return NoContent();
    }

    private bool TryGetUserId(out Guid userId)
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(raw, out userId);
    }
}

public sealed record GenerateOwnerCodeRequest(Guid ClinicId);
public sealed record AcceptCodeRequest(string Code);
