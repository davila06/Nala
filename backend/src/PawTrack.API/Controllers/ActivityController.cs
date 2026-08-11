using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PawTrack.Application.Medical;
using PawTrack.Domain.Medical;
using System.Security.Claims;

namespace PawTrack.API.Controllers;

[ApiController]
[Route("api/pets/{petId:guid}/activity")]
[Authorize]
public sealed class ActivityController(ISender sender) : ControllerBase
{
    // ── GET /api/pets/{petId}/activity?from=&to= ──────────────────────────────
    [HttpGet]
    [EnableRateLimiting("public-api")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetLogs(
        Guid petId,
        [FromQuery] string? from,
        [FromQuery] string? to,
        CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        DateOnly? fromDate = from is not null && DateOnly.TryParse(from, out var f) ? f : null;
        DateOnly? toDate   = to   is not null && DateOnly.TryParse(to,   out var t) ? t : null;

        var result = await sender.Send(new GetActivityLogsQuery(petId, userId, fromDate, toDate), ct);
        if (result.IsFailure)
        {
            if (result.Errors.Contains("El historial de actividad requiere el plan Plus."))
                return StatusCode(StatusCodes.Status403Forbidden,
                    new ProblemDetails { Title = "Plan required", Detail = result.Errors.First(), Status = 403 });
            return UnprocessableEntity(new ProblemDetails { Detail = string.Join("; ", result.Errors), Status = 422 });
        }
        return Ok(result.Value);
    }

    // ── POST /api/pets/{petId}/activity ───────────────────────────────────────
    [HttpPost]
    [EnableRateLimiting("public-api")]
    [RequestSizeLimit(512)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Log(
        Guid petId,
        [FromBody] LogActivityRequest request,
        CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        if (!Enum.TryParse<ActivityType>(request.Type, ignoreCase: true, out var type))
            return BadRequest(new ProblemDetails { Detail = $"Tipo de actividad inválido: {request.Type}.", Status = 400 });
        if (!DateOnly.TryParse(request.Date, out var date))
            return BadRequest(new ProblemDetails { Detail = "Fecha inválida.", Status = 400 });

        var result = await sender.Send(
            new LogActivityCommand(petId, userId, date, type,
                request.DurationMinutes, request.DistanceMeters, request.Notes), ct);

        if (result.IsFailure)
            return UnprocessableEntity(new ProblemDetails { Detail = string.Join("; ", result.Errors), Status = 422 });
        return Created(string.Empty, result.Value);
    }

    // ── DELETE /api/pets/{petId}/activity/{id} ────────────────────────────────
    [HttpDelete("{id:guid}")]
    [EnableRateLimiting("public-api")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid petId, Guid id, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await sender.Send(new DeleteActivityLogCommand(id, userId), ct);
        if (result.IsFailure)
            return UnprocessableEntity(new ProblemDetails { Detail = string.Join("; ", result.Errors), Status = 422 });
        return NoContent();
    }

    private bool TryGetUserId(out Guid userId)
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(claim, out userId);
    }
}

public sealed record LogActivityRequest(
    string Date,
    string Type,
    int DurationMinutes,
    int? DistanceMeters,
    string? Notes);
