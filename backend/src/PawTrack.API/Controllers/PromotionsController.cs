using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PawTrack.Application.Promotions;
using System.Security.Claims;

namespace PawTrack.API.Controllers;

/// <summary>Public + authenticated promotion code endpoints.</summary>
[ApiController]
[Route("api/promotions")]
public sealed class PromotionsController(ISender sender) : ControllerBase
{
    // ── GET /api/promotions/validate/{code} — no auth required ───────────────
    [HttpGet("validate/{code}")]
    [AllowAnonymous]
    [EnableRateLimiting("public-api")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Validate(string code, CancellationToken ct)
    {
        var result = await sender.Send(new ValidatePromotionCodeQuery(code), ct);
        if (result.IsFailure)
            return UnprocessableEntity(new ProblemDetails { Detail = result.Errors.FirstOrDefault(), Status = 422 });
        return Ok(result.Value);
    }

    // ── POST /api/promotions/redeem — auth required ───────────────────────────
    [HttpPost("redeem")]
    [Authorize]
    [EnableRateLimiting("public-api")]
    [RequestSizeLimit(512)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Redeem([FromBody] RedeemRequest request, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var tier = request.SelectedTier is not null &&
                   Enum.TryParse<PawTrack.Domain.Subscriptions.SubscriptionTier>(request.SelectedTier, out var t)
            ? t : (PawTrack.Domain.Subscriptions.SubscriptionTier?)null;

        var result = await sender.Send(new RedeemPromotionCodeCommand(request.Code, userId, tier), ct);
        if (result.IsFailure)
            return UnprocessableEntity(new ProblemDetails { Detail = result.Errors.FirstOrDefault(), Status = 422 });
        return Ok(result.Value);
    }

    private bool TryGetUserId(out Guid userId)
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(raw, out userId);
    }
}

public sealed record RedeemRequest(string Code, string? SelectedTier);
