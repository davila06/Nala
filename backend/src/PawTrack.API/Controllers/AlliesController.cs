using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PawTrack.Application.Allies.Commands.ConfirmAllyAlertAction;
using PawTrack.Application.Allies.Commands.ReviewAllyApplication;
using PawTrack.Application.Allies.Commands.SubmitAllyApplication;
using PawTrack.Application.Allies.Queries.GetMyAllyAlerts;
using PawTrack.Application.Allies.Queries.GetMyAllyProfile;
using PawTrack.Application.Allies.Queries.GetPendingAllies;
using PawTrack.Domain.Allies;
using System.Security.Claims;

namespace PawTrack.API.Controllers;

[ApiController]
[Route("api/allies")]
[Authorize]
public sealed class AlliesController(ISender sender) : ControllerBase
{
    [HttpGet("me")]
    [EnableRateLimiting("public-api")] // 30/min — each call issues GetMyAllyProfileQuery (DB SELECT)
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyProfile(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        var result = await sender.Send(new GetMyAllyProfileQuery(userId), cancellationToken);
        return Ok(result.Value);
    }

    [HttpPost("me/application")]
    [EnableRateLimiting("public-api")] // 30/min — prevents spam-flooding ally applications
    [RequestSizeLimit(8192)] // 8 KB — org name + coords + metadata; no media content
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SubmitApplication(
        [FromBody] SubmitAllyApplicationRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        var result = await sender.Send(new SubmitAllyApplicationCommand(
            userId,
            request.OrganizationName,
            request.AllyType,
            request.CoverageLabel,
            request.CoverageLat,
            request.CoverageLng,
            request.CoverageRadiusMetres), cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Ally application failed",
                Detail = string.Join("; ", result.Errors),
                Status = 400,
            });
        }

        return Ok(result.Value);
    }

    [HttpGet("me/alerts")]
    [EnableRateLimiting("public-api")] // 30/min — each call issues GetMyAllyAlertsQuery (DB SELECT)
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetMyAlerts(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        var result = await sender.Send(new GetMyAllyAlertsQuery(userId), cancellationToken);
        if (result.IsFailure)
        {
            return Forbid();
        }

        return Ok(result.Value);
    }

    [HttpPut("me/alerts/{notificationId:guid}/action")]
    [EnableRateLimiting("public-api")] // 30/min — each call fires 2 SELECTs + 1 UPDATE + SaveChangesAsync
    [RequestSizeLimit(4096)]           // ActionSummary ≤ 280 chars; 4 KB ceiling stops oversized JSON
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ConfirmAction(
        Guid notificationId,
        [FromBody] ConfirmAllyAlertActionRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        var result = await sender.Send(
            new ConfirmAllyAlertActionCommand(notificationId, userId, request.ActionSummary),
            cancellationToken);

        if (result.IsFailure)
        {
            if (result.Errors.Contains("Notification not found."))
            {
                return NotFound(new ProblemDetails { Title = "Notification not found", Status = 404 });
            }

            if (result.Errors.Contains("Access denied.") || result.Errors.Any(x => x.Contains("verified allies")))
            {
                return Forbid();
            }

            return BadRequest(new ProblemDetails
            {
                Title = "Action confirmation failed",
                Detail = string.Join("; ", result.Errors),
                Status = 400,
            });
        }

        return NoContent();
    }

    [HttpGet("admin/pending")]
    [Authorize(Roles = "Admin")]  // framework-level: rejects non-admin before action body runs
    [EnableRateLimiting("public-api")] // 30/min — Admin-only but unthrottled DB SELECT still opens DoS vector
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetPendingApplications(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetPendingAlliesQuery(), cancellationToken);
        return Ok(result.Value);
    }

    [HttpPost("admin/applications/{userId:guid}/review")]
    [Authorize(Roles = "Admin")]  // framework-level: rejects non-admin before action body runs
    [EnableRateLimiting("public-api")] // 30/min — each call writes ReviewAllyApplicationCommand (DB write)
    [RequestSizeLimit(128)] // single bool — max ~20 B; 128 B ceiling
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ReviewApplication(
        Guid userId,
        [FromBody] ReviewAllyApplicationRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ReviewAllyApplicationCommand(userId, request.Approve), cancellationToken);
        if (result.IsFailure)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Ally application not found",
                Detail = string.Join("; ", result.Errors),
                Status = 404,
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

public sealed record SubmitAllyApplicationRequest(
    string OrganizationName,
    AllyType AllyType,
    string CoverageLabel,
    double CoverageLat,
    double CoverageLng,
    int CoverageRadiusMetres);

public sealed record ConfirmAllyAlertActionRequest(string ActionSummary);

public sealed record ReviewAllyApplicationRequest(bool Approve);