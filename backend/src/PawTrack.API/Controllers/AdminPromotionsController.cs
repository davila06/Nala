using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Authentication;
using PawTrack.Application.Promotions;
using PawTrack.Domain.Promotions;
using PawTrack.Domain.Subscriptions;
using System.Security.Claims;

namespace PawTrack.API.Controllers;

/// <summary>Admin-only promotion code management.</summary>
[ApiController]
[Route("api/admin/promotions")]
[Authorize(Roles = "Admin")]
public sealed class AdminPromotionsController(ISender sender) : ControllerBase
{
    // ── GET /api/admin/promotions ─────────────────────────────────────────────
    [HttpGet]
    [EnableRateLimiting("public-api")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var result = await sender.Send(new GetAllPromotionCodesQuery(), ct);
        return result.IsSuccess ? Ok(result.Value) : Problem(detail: result.Errors.FirstOrDefault());
    }

    // ── POST /api/admin/promotions/batch ──────────────────────────────────────
    [HttpPost("batch")]
    [EnableRateLimiting("public-api")]
    [RequestSizeLimit(32_768)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> CreateBatch(
        [FromBody] AdminCreateBatchRequest request,
        CancellationToken ct)
    {
        if (!TryGetAdminId(out var adminId)) return Unauthorized();

        var specs = request.Specs.Select(s => new PromotionCodeSpec(
            ParseEnum<PromotionType>(s.Type),
            s.DiscountPercent,
            s.FreeMonths,
            s.TargetTier is null ? null : ParseEnum<SubscriptionTier>(s.TargetTier),
            s.MaxRedemptions,
            s.ExpiresAt,
            s.AdminNote,
            s.Quantity)).ToList();

        var result = await sender.Send(new CreatePromotionBatchCommand(adminId, specs), ct);
        if (result.IsFailure)
            return UnprocessableEntity(new ProblemDetails { Detail = result.Errors.FirstOrDefault(), Status = 422 });
        return StatusCode(201, result.Value);
    }

    // ── PUT /api/admin/promotions/{id}/toggle ─────────────────────────────────
    [HttpPut("{id:guid}/toggle")]
    [EnableRateLimiting("public-api")]
    [RequestSizeLimit(256)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Toggle(
        Guid id, [FromBody] ToggleRequest request, CancellationToken ct)
    {
        if (!TryGetAdminId(out var adminId)) return Unauthorized();
        var result = await sender.Send(new TogglePromotionCodeCommand(id, request.Activate, adminId), ct);
        if (result.IsFailure)
            return UnprocessableEntity(new ProblemDetails { Detail = result.Errors.FirstOrDefault(), Status = 422 });
        return Ok(result.Value);
    }

    private bool TryGetAdminId(out Guid adminId)
    {
        var raw = User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
        return Guid.TryParse(raw, out adminId);
    }

    private static T ParseEnum<T>(string value) where T : struct, Enum =>
        Enum.Parse<T>(value, ignoreCase: true);
}

public sealed record AdminCreateBatchRequest(IReadOnlyList<PromotionSpecRequest> Specs);

public sealed record PromotionSpecRequest(
    string Type,
    int? DiscountPercent,
    int? FreeMonths,
    string? TargetTier,
    int MaxRedemptions,
    DateTimeOffset? ExpiresAt,
    string? AdminNote,
    int Quantity);

public sealed record ToggleRequest(bool Activate);
