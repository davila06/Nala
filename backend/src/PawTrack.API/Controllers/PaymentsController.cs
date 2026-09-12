using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PawTrack.Application.Payments.Commands.ChargeCard;
using PawTrack.Application.Payments.Commands.DeletePaymentProfile;
using PawTrack.Application.Payments.Commands.SavePaymentProfile;
using PawTrack.Application.Payments.DTOs;
using PawTrack.Application.Payments.Queries.GetCaptureContext;
using PawTrack.Application.Payments.Queries.GetUserPaymentProfiles;

namespace PawTrack.API.Controllers;

[ApiController]
[Route("api/payments")]
[Authorize]
public sealed class PaymentsController(ISender sender) : ControllerBase
{
    // ── GET /api/payments/capture-context — Microform JWT & SDK url ──────────
    [HttpGet("capture-context")]
    [EnableRateLimiting("public-api")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCaptureContext(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var result = await sender.Send(new GetCaptureContextQuery(userId), cancellationToken);
        return Ok(result.Value);
    }

    // ── GET /api/payments/profiles — User's saved cards ──────────────────────
    [HttpGet("profiles")]
    [EnableRateLimiting("public-api")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProfiles(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var result = await sender.Send(new GetUserPaymentProfilesQuery(userId), cancellationToken);
        return Ok(result.Value);
    }

    // ── POST /api/payments/profiles — Tokenize and save card ─────────────────
    [HttpPost("profiles")]
    [EnableRateLimiting("public-api")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> SaveProfile(
        [FromBody] SavePaymentProfileRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var result = await sender.Send(
            new SavePaymentProfileCommand(userId, request.TransientToken, request.CardholderName, request.SetAsDefault),
            cancellationToken);

        if (result.IsFailure)
            return UnprocessableEntity(new ProblemDetails { Detail = string.Join(", ", result.Errors) });

        return Created($"/api/payments/profiles/{result.Value.Id}", result.Value);
    }

    // ── DELETE /api/payments/profiles/{id} — Remove saved card ───────────────
    [HttpDelete("profiles/{id:guid}")]
    [EnableRateLimiting("public-api")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteProfile(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var result = await sender.Send(new DeletePaymentProfileCommand(id, userId), cancellationToken);
        if (result.IsFailure)
            return NotFound(new ProblemDetails { Detail = string.Join(", ", result.Errors) });

        return NoContent();
    }

    // ── POST /api/payments/charge — Charge card for Subscription or Bundle ───
    [HttpPost("charge")]
    [EnableRateLimiting("public-api")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ChargeCard(
        [FromBody] ChargeCardRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var result = await sender.Send(
            new ChargeCardCommand(
                UserId: userId,
                AmountCrc: request.AmountCrc,
                Purpose: request.Purpose,
                TargetEntityId: request.TargetEntityId,
                PaymentProfileId: request.PaymentProfileId,
                TransientToken: request.TransientToken,
                CardholderName: request.CardholderName,
                SaveProfile: request.SaveProfile),
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

public sealed record SavePaymentProfileRequest(
    string TransientToken,
    string? CardholderName = null,
    bool SetAsDefault = true);

public sealed record ChargeCardRequest(
    decimal AmountCrc,
    string Purpose,
    Guid? TargetEntityId = null,
    Guid? PaymentProfileId = null,
    string? TransientToken = null,
    string? CardholderName = null,
    bool SaveProfile = false);
