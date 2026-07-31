using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PawTrack.Application.Bounties.Commands.ConfirmBountyDeposit;
using PawTrack.Application.Bounties.Commands.CreateBounty;
using PawTrack.Application.Bounties.Commands.ReleaseBounty;
using PawTrack.Application.Bounties.Queries.GetBountyForEvent;
using System.Security.Claims;

namespace PawTrack.API.Controllers;

[ApiController]
[Route("api/bounties")]
[Authorize]
public sealed class BountiesController(ISender sender) : ControllerBase
{
    // ── GET /api/bounties/event/{lostEventId} ─────────────────────────────────
    [HttpGet("event/{lostEventId:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetForEvent(Guid lostEventId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetBountyForEventQuery(lostEventId), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : NotFound();
    }

    // ── POST /api/bounties ────────────────────────────────────────────────────
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Create(
        [FromBody] CreateBountyRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var result = await sender.Send(
            new CreateBountyCommand(request.LostPetEventId, userId, request.Amount, request.CurrencyCode ?? "CRC"),
            cancellationToken);

        if (result.IsFailure)
            return UnprocessableEntity(new ProblemDetails { Detail = string.Join(", ", result.Errors) });

        return Created($"api/bounties/event/{request.LostPetEventId}", result.Value);
    }

    // ── PUT /api/bounties/confirm-deposit ─────────────────────────────────────
    [HttpPut("confirm-deposit")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ConfirmDeposit(
        [FromBody] ConfirmDepositRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ConfirmBountyDepositCommand(request.DepositReference), cancellationToken);

        if (result.IsFailure)
            return UnprocessableEntity(new ProblemDetails { Detail = string.Join(", ", result.Errors) });

        return Ok(result.Value);
    }

    // ── PUT /api/bounties/{id}/release ────────────────────────────────────────
    [HttpPut("{id:guid}/release")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Release(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var result = await sender.Send(new ReleaseBountyCommand(id, userId), cancellationToken);

        if (result.IsFailure)
            return UnprocessableEntity(new ProblemDetails { Detail = string.Join(", ", result.Errors) });

        return Ok(result.Value);
    }

    private bool TryGetUserId(out Guid userId)
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(raw, out userId);
    }
}

public sealed record CreateBountyRequest(Guid LostPetEventId, decimal Amount, string? CurrencyCode);
public sealed record ConfirmDepositRequest(string DepositReference);
