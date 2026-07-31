using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PawTrack.Application.Municipalities.Commands.RecordCapture;
using PawTrack.Application.Municipalities.Commands.UpdateCaptureStatus;
using PawTrack.Application.Municipalities.Queries.SearchCaptures;
using PawTrack.Domain.Municipalities;
using System.Security.Claims;

namespace PawTrack.API.Controllers;

[ApiController]
[Route("api/municipalities")]
[Authorize]
public sealed class MunicipalController(ISender sender) : ControllerBase
{
    // ── GET /api/municipalities/captures?canton=&status=&page=&pageSize= ─────
    [HttpGet("captures")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Search(
        [FromQuery] string?              canton,
        [FromQuery] CapturedAnimalStatus? status,
        [FromQuery] int page     = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(
            new SearchCapturesQuery(canton, status, page, pageSize),
            cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Errors);
    }

    // ── POST /api/municipalities/captures ─────────────────────────────────────
    [HttpPost("captures")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Record(
        [FromBody] RecordCaptureRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var result = await sender.Send(new RecordCaptureCommand(
            userId,
            request.Canton,
            request.Species,
            request.Color,
            request.Breed,
            request.EstimatedAge,
            request.Notes,
            request.CollarChipNumber,
            request.CapturedAt), cancellationToken);

        if (result.IsFailure)
            return UnprocessableEntity(new ProblemDetails { Detail = string.Join(", ", result.Errors) });

        return Created($"api/municipalities/captures/{result.Value!.Id}", result.Value);
    }

    // ── PUT /api/municipalities/captures/{id}/status ──────────────────────────
    [HttpPut("captures/{id:guid}/status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> UpdateStatus(
        Guid id,
        [FromBody] UpdateStatusRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new UpdateCaptureStatusCommand(id, request.Status, request.MatchedPetId),
            cancellationToken);

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

public sealed record RecordCaptureRequest(
    string Canton, string Species, string Color,
    string? Breed, string? EstimatedAge, string? Notes,
    string? CollarChipNumber, DateTimeOffset? CapturedAt);

public sealed record UpdateStatusRequest(CapturedAnimalStatus Status, Guid? MatchedPetId);
