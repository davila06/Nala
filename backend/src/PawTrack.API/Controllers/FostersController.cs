using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PawTrack.Application.Fosters.Commands.CloseCustody;
using PawTrack.Application.Fosters.Commands.StartCustody;
using PawTrack.Application.Fosters.Commands.UpsertMyFosterProfile;
using PawTrack.Application.Fosters.Queries.GetFosterSuggestions;
using PawTrack.Application.Fosters.Queries.GetMyFosterProfile;
using PawTrack.Domain.Pets;
using System.Security.Claims;

namespace PawTrack.API.Controllers;

[ApiController]
[Route("api/fosters")]
public sealed class FostersController(ISender sender) : ControllerBase
{
    [HttpGet("me")]
    [Authorize]
    [EnableRateLimiting("public-api")] // 30/min — each call issues GetMyFosterProfileQuery (DB SELECT)
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMyProfile(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        var result = await sender.Send(new GetMyFosterProfileQuery(userId), cancellationToken);
        return result.IsFailure
            ? NotFound(new ProblemDetails { Title = "Foster profile not found", Status = 404 })
            : Ok(result.Value);
    }

    [HttpPut("me")]
    [Authorize]
    [EnableRateLimiting("public-api")] // 30/min — writes precise HomeLat/HomeLng PII to DB on every call
    [RequestSizeLimit(8192)]           // lat+lng+enum array+int+bool+timestamp; 8 KB stops oversized payloads
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> UpsertMyProfile(
        [FromBody] UpsertFosterProfileRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        var command = new UpsertMyFosterProfileCommand(
            userId,
            request.HomeLat,
            request.HomeLng,
            request.AcceptedSpecies,
            request.SizePreference,
            request.MaxDays,
            request.IsAvailable,
            request.AvailableUntil);

        var result = await sender.Send(command, cancellationToken);
        if (result.IsFailure)
        {
            return UnprocessableEntity(new ProblemDetails
            {
                Title = "Validation error",
                Status = 422,
                Extensions = { ["errors"] = result.Errors },
            });
        }

        return NoContent();
    }

    [HttpGet("suggestions/from-found-report/{foundReportId:guid}")]
    [Authorize]                        // authenticated only — prevents anonymous volunteer enumeration
    [EnableRateLimiting("public-api")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSuggestions(
        Guid foundReportId,
        [FromQuery] int maxResults = 3,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetFosterSuggestionsQuery(foundReportId, maxResults), cancellationToken);

        return result.IsFailure
            ? NotFound(new ProblemDetails { Title = "Found report not found", Status = 404 })
            : Ok(result.Value);
    }

    [HttpPost("custody-records/start")]
    [Authorize]
    [EnableRateLimiting("public-api")] // 30/min — each call inserts a new CustodyRecord row
    [RequestSizeLimit(4096)]           // GUID + int + Note (≤ 500 chars); 4 KB ceiling
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> StartCustody(
        [FromBody] StartCustodyRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        var result = await sender.Send(new StartCustodyCommand(
            userId,
            request.FoundPetReportId,
            request.ExpectedDays,
            request.Note), cancellationToken);

        if (result.IsFailure)
        {
            return UnprocessableEntity(new ProblemDetails
            {
                Title = "Cannot start custody",
                Status = 422,
                Extensions = { ["errors"] = result.Errors },
            });
        }

        return Ok(new { recordId = result.Value });
    }

    [HttpPatch("custody-records/{id:guid}/close")]
    [Authorize]
    [EnableRateLimiting("public-api")] // 30/min — triggers domain state-machine + SaveChangesAsync
    [RequestSizeLimit(2048)]           // outcome enum string only; 2 KB ceiling
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> CloseCustody(
        Guid id,
        [FromBody] CloseCustodyRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        var result = await sender.Send(new CloseCustodyCommand(id, userId, request.Outcome), cancellationToken);

        if (result.IsFailure)
        {
            return UnprocessableEntity(new ProblemDetails
            {
                Title = "Cannot close custody",
                Status = 422,
                Extensions = { ["errors"] = result.Errors },
            });
        }

        return NoContent();
    }

    private bool TryGetUserId(out Guid userId)
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? User.FindFirstValue("sub");
        return Guid.TryParse(claim, out userId);
    }
}

public sealed record UpsertFosterProfileRequest(
    double HomeLat,
    double HomeLng,
    IReadOnlyList<PetSpecies> AcceptedSpecies,
    string? SizePreference,
    int MaxDays,
    bool IsAvailable,
    DateTimeOffset? AvailableUntil);

public sealed record StartCustodyRequest(
    Guid FoundPetReportId,
    int ExpectedDays,
    string? Note);

public sealed record CloseCustodyRequest(string Outcome);
