using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PawTrack.Application.Family;
using System.Security.Claims;

namespace PawTrack.API.Controllers;

[ApiController]
[Route("api/family")]
[Authorize]
public sealed class FamilyController(ISender sender) : ControllerBase
{
    // ── GET /api/family ───────────────────────────────────────────────────────
    [HttpGet]
    [EnableRateLimiting("public-api")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyFamily(CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await sender.Send(new GetFamilyMembersQuery(userId), ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest();
    }

    // ── POST /api/family ──────────────────────────────────────────────────────
    [HttpPost]
    [EnableRateLimiting("public-api")]
    [RequestSizeLimit(1024)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> CreateAccount(
        [FromBody] CreateFamilyAccountRequest request, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await sender.Send(new CreateFamilyAccountCommand(userId, request.Name), ct);
        if (result.IsFailure)
            return UnprocessableEntity(new ProblemDetails { Detail = string.Join("; ", result.Errors), Status = 422 });
        return Created(string.Empty, result.Value);
    }

    // ── POST /api/family/invite ───────────────────────────────────────────────
    [HttpPost("invite")]
    [EnableRateLimiting("public-api")]
    [RequestSizeLimit(512)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Invite(
        [FromBody] InviteMemberRequest request, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await sender.Send(new InviteFamilyMemberCommand(userId, request.Email), ct);
        if (result.IsFailure)
            return UnprocessableEntity(new ProblemDetails { Detail = string.Join("; ", result.Errors), Status = 422 });
        return Ok(result.Value);
    }

    // ── POST /api/family/invitations/{token}/accept ───────────────────────────
    [HttpPost("invitations/{token:guid}/accept")]
    [EnableRateLimiting("public-api")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> AcceptInvitation(Guid token, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await sender.Send(new AcceptFamilyInvitationCommand(userId, token), ct);
        if (result.IsFailure)
            return UnprocessableEntity(new ProblemDetails { Detail = string.Join("; ", result.Errors), Status = 422 });
        return NoContent();
    }

    // ── DELETE /api/family/members/{userId} ───────────────────────────────────
    [HttpDelete("members/{memberId:guid}")]
    [EnableRateLimiting("public-api")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> RemoveMember(Guid memberId, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await sender.Send(new RemoveFamilyMemberCommand(userId, memberId), ct);
        if (result.IsFailure)
            return UnprocessableEntity(new ProblemDetails { Detail = string.Join("; ", result.Errors), Status = 422 });
        return NoContent();
    }

    private bool TryGetUserId(out Guid userId)
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(raw, out userId);
    }
}

public sealed record CreateFamilyAccountRequest(string Name);
public sealed record InviteMemberRequest(string Email);
