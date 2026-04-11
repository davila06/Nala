using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PawTrack.Application.Safety.Commands.ReportFraud;
using PawTrack.Domain.Safety;
using System.Security.Claims;

namespace PawTrack.API.Controllers;

/// <summary>
/// Allows users and anonymous reporters to flag fraudulent or suspicious behaviour.
/// Anonymous reports are rate-limited via the "sightings" policy (shared bucket).
/// </summary>
[ApiController]
[Route("api/fraud-reports")]
public sealed class FraudReportController(ISender sender) : ControllerBase
{
    // ── POST /api/fraud-reports ───────────────────────────────────────────────

    /// <summary>
    /// Submits a fraud or scam attempt report.
    /// Authenticated users are trusted; anonymous reporters are rate-limited by IP.
    /// </summary>
    [HttpPost]
    [AllowAnonymous]
    [EnableRateLimiting("sightings")] // reuse same bucket — low-throughput public action
    [RequestSizeLimit(4096)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ReportFraud(
        [FromBody] ReportFraudRequest request,
        CancellationToken cancellationToken)
    {
        Guid? reporterUserId = null;

        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? User.FindFirstValue("sub");

        if (Guid.TryParse(claim, out var parsedId))
            reporterUserId = parsedId;

        // After UseForwardedHeaders() middleware (Program.cs, KnownNetworks scoped
        // to RFC-1918 ranges), RemoteIpAddress holds the real client IP.
        // Never read X-Forwarded-For directly — it is trivially spoofable.
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        if (!Enum.TryParse<FraudContext>(request.Context, ignoreCase: true, out var context))
            return UnprocessableEntity(new ProblemDetails
            {
                Title  = "Contexto inválido",
                Status = 422,
                Extensions = { ["errors"] = new[] { "Valor de 'context' no reconocido." } },
            });

        var result = await sender.Send(
            new ReportFraudCommand(
                reporterUserId,
                ip,
                context,
                request.RelatedEntityId,
                request.TargetUserId,
                request.Description),
            cancellationToken);

        if (result.IsFailure)
            return UnprocessableEntity(new ProblemDetails
            {
                Title  = "No se pudo registrar el reporte",
                Status = 422,
                Extensions = { ["errors"] = result.Errors },
            });

        return Ok(new
        {
            message         = result.Value!.Message,
            suspicionLevel  = result.Value.SuspicionLevel.ToString(),
        });
    }
}

// ── Request model ──────────────────────────────────────────────────────────────

/// <param name="Context">One of: PublicProfile, ChatMessage, PhoneContact, Other.</param>
/// <param name="RelatedEntityId">LostPetEventId or ChatThreadId (optional).</param>
/// <param name="TargetUserId">The user being reported (optional).</param>
/// <param name="Description">Free-text description of what happened (max 500 chars).</param>
public sealed record ReportFraudRequest(
    string  Context,
    Guid?   RelatedEntityId,
    Guid?   TargetUserId,
    string? Description);
