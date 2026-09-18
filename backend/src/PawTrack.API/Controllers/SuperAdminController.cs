using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PawTrack.Application.Auth.Commands.AssignSuperAdminRole;

namespace PawTrack.API.Controllers;

/// <summary>Exceptional privileged-access operations restricted to MFA-authenticated SuperAdmins.</summary>
[ApiController]
[Route("api/super-admin")]
[Authorize(Policy = "SuperAdmin")]
public sealed class SuperAdminController(ISender sender) : ControllerBase
{
    /// <summary>Elevates a verified, MFA-enabled account to SuperAdmin and writes an immutable audit record.</summary>
    [HttpPut("users/{userId:guid}/role")]
    [EnableRateLimiting("change-password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Assign(Guid userId, [FromBody] AssignSuperAdminRequest request, CancellationToken ct)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var actorId))
            return Unauthorized();

        var result = await sender.Send(new AssignSuperAdminRoleCommand(actorId, userId, request.Reason, request.MfaCode), ct);
        if (result.IsFailure)
            return UnprocessableEntity(new ProblemDetails { Detail = string.Join("; ", result.Errors), Status = 422 });
        return NoContent();
    }

    /// <summary>Revokes SuperAdmin to Admin while preventing self-revocation and removal of the last SuperAdmin.</summary>
    [HttpDelete("users/{userId:guid}/role")]
    [EnableRateLimiting("change-password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Revoke(Guid userId, [FromBody] AssignSuperAdminRequest request, CancellationToken ct)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var actorId))
            return Unauthorized();
        var result = await sender.Send(new RevokeSuperAdminRoleCommand(actorId, userId, request.Reason, request.MfaCode), ct);
        if (result.IsFailure)
            return UnprocessableEntity(new ProblemDetails { Detail = string.Join("; ", result.Errors), Status = 422 });
        return NoContent();
    }
}

/// <summary>Reason and approval reference for a privileged role elevation.</summary>
public sealed record AssignSuperAdminRequest(string Reason, string MfaCode);
