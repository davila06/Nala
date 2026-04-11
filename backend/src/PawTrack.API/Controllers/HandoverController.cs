using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PawTrack.Application.Safety.Commands.GenerateHandoverCode;
using PawTrack.Application.Safety.Commands.VerifyHandoverCode;
using System.Security.Claims;

namespace PawTrack.API.Controllers;

/// <summary>
/// Manages 4-digit handover codes for safe physical pet reunification.
/// The owner generates the code; the rescuer submits it on arrival to confirm the handover.
/// </summary>
[ApiController]
[Route("api/lost-pets/{lostPetEventId:guid}/handover")]
[Authorize]
public sealed class HandoverController(ISender sender) : ControllerBase
{
    // ── POST /api/lost-pets/{id}/handover/code — owner generates code ─────────

    /// <summary>
    /// Generates a fresh 4-digit handover code for the specified lost-pet event.
    /// Only the pet owner may call this.  Any previously active code is superseded.
    /// </summary>
    [HttpPost("code")]
    [EnableRateLimiting("public-api")] // 30/min — prevents rapid code-cycling that would invalidate legitimate handovers
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> GenerateCode(
        Guid lostPetEventId,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var result = await sender.Send(
            new GenerateHandoverCodeCommand(lostPetEventId, userId),
            cancellationToken);

        if (result.IsFailure)
        {
            return result.Errors.Contains("Solo el dueño puede generar el código de entrega.")
                ? Forbid()
                : UnprocessableEntity(new ProblemDetails
                {
                    Title  = "No se pudo generar el código",
                    Status = 422,
                    Extensions = { ["errors"] = result.Errors },
                });
        }

        // Return the code once; client must display it to the owner securely.
        return Ok(new { code = result.Value, expiresInHours = 24 });
    }

    // ── POST /api/lost-pets/{id}/handover/verify — rescuer verifies code ──────

    /// <summary>
    /// Rescuer submits the 4-digit code received from the owner to confirm handover.
    /// Returns <c>{ verified: true }</c> on success, <c>{ verified: false }</c> on invalid/expired code.
    /// </summary>
    [HttpPost("verify")]
    [EnableRateLimiting("handover-verify")]  // 5 attempts/min per user — brute-force guard
    [RequestSizeLimit(512)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> VerifyCode(
        Guid lostPetEventId,
        [FromBody] VerifyHandoverCodeRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var result = await sender.Send(
            new VerifyHandoverCodeCommand(lostPetEventId, userId, request.Code),
            cancellationToken);

        if (result.IsFailure)
            return UnprocessableEntity(new ProblemDetails
            {
                Title  = "Verificación fallida",
                Status = 422,
                Extensions = { ["errors"] = result.Errors },
            });

        return Ok(new { verified = result.Value });
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private bool TryGetUserId(out Guid userId)
    {
        userId = Guid.Empty;
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? User.FindFirstValue("sub");
        return Guid.TryParse(claim, out userId);
    }
}

// ── Request model ──────────────────────────────────────────────────────────────

public sealed record VerifyHandoverCodeRequest(string Code);
