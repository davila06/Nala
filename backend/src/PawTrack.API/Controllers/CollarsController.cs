using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PawTrack.Application.Collars.Commands.RegisterCollar;
using PawTrack.Application.Collars.Interfaces;
using PawTrack.Application.Collars.Queries.GetCollarStatus;
using PawTrack.Application.Collars.Queries.GetLocationHistory;
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
    [EnableRateLimiting("public-api")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStatus(Guid petId, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await sender.Send(new GetCollarStatusQuery(petId, userId), cancellationToken);
        if (result.IsFailure)
            return result.Errors.Contains("Access denied.") ? Forbid() : NotFound();
        return Ok(result.Value);
    }

    // ── POST /api/collars ────────────────────────────────────────────────────
    [HttpPost]
    [EnableRateLimiting("public-api")]
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

    // ── GET /api/collars/pet/{petId}/history?hours=24 ────────────────────────
    [HttpGet("pet/{petId:guid}/history")]
    [EnableRateLimiting("public-api")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetHistory(
        Guid petId,
        [FromQuery] int hours = 24,
        [FromQuery] int maxPoints = 500,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await sender.Send(new GetLocationHistoryQuery(petId, userId, hours, maxPoints), cancellationToken);
        if (result.IsFailure)
            return result.Errors.Contains("Access denied.") ? Forbid() : BadRequest(result.Errors);
        return Ok(result.Value);
    }

    // ── POST /api/collars/pet/{petId}/location ────────────────────────────────
    /// <summary>Manual location record — for generic/own hardware via HTTP push.</summary>
    [HttpPost("pet/{petId:guid}/location")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RecordLocation(
        Guid petId,
        [FromBody] RecordLocationRequest request,
        [FromServices] ICollarRepository collarRepository,
        [FromServices] PawTrack.Application.Common.Interfaces.IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        var collar = await collarRepository.GetActiveForPetAsync(petId, cancellationToken);
        if (collar is null) return NotFound();

        collar.UpdateLocation(request.Lat, request.Lng, request.BatteryPercent);
        collarRepository.Update(collar);
        await collarRepository.AddLocationAsync(
            CollarLocation.Record(collar.Id, request.Lat, request.Lng, request.Accuracy),
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    // ── GET /api/collars/tractive/connect?petId={petId} ───────────────────────
    /// <summary>Initiates Tractive OAuth2 flow. Redirects to Tractive consent screen.</summary>
    [HttpGet("tractive/connect")]
    [ProducesResponseType(StatusCodes.Status302Found)]
    public IActionResult ConnectTraactive([FromQuery] Guid petId, [FromServices] ITractiveService tractive)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var state = $"{userId}:{petId}";
        var authUrl = tractive.GetAuthorizationUrl(state);
        return Redirect(authUrl);
    }

    // ── GET /api/collars/tractive/callback ────────────────────────────────────
    [HttpGet("tractive/callback")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status302Found)]
    public async Task<IActionResult> TractiveCallback(
        [FromQuery] string code,
        [FromQuery] string state,
        [FromServices] ITractiveService tractive,
        [FromServices] ICollarRepository collarRepository,
        CancellationToken cancellationToken)
    {
        var parts = state.Split(':');
        if (parts.Length != 2
            || !Guid.TryParse(parts[0], out var userId)
            || !Guid.TryParse(parts[1], out var petId))
            return BadRequest("Invalid state.");

        var encryptedToken = await tractive.ExchangeCodeForTokenAsync(code, cancellationToken);

        var collar = await collarRepository.GetActiveForPetAsync(petId, cancellationToken);
        if (collar is not null && collar.Provider == Domain.Collars.CollarProvider.Tractive)
        {
            collar.SetToken(encryptedToken);
            collarRepository.Update(collar);
        }

        // Redirect to frontend GPS tab after OAuth
        return Redirect($"https://pawtrack.cr/pets/{petId}?tab=gps&connected=true");
    }

    private bool TryGetUserId(out Guid userId)
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(raw, out userId);
    }
}

public sealed record RegisterCollarRequest(Guid PetId, CollarProvider Provider, string? ExternalDeviceId);
public sealed record RecordLocationRequest(double Lat, double Lng, int? BatteryPercent, int? Accuracy);
