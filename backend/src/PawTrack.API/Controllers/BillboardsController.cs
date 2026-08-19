using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PawTrack.Application.Advertising;
using System.Security.Claims;

namespace PawTrack.API.Controllers;

[ApiController]
[Route("api/billboards")]
public sealed class BillboardsController(ISender sender) : ControllerBase
{
    // ── GET /api/billboards?placement=Map — public, returns active ads ─────────
    [HttpGet]
    [AllowAnonymous]
    [EnableRateLimiting("public-api")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetActive(
        [FromQuery] string placement = "Map",
        CancellationToken ct = default)
    {
        var result = await sender.Send(new GetActiveBillboardsQuery(placement), ct);
        return Ok(result);
    }

    // ── GET /api/billboards/admin — Admin: paginated list ─────────────────────
    [HttpGet("admin")]
    [Authorize(Roles = "Admin")]
    [EnableRateLimiting("public-api")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await sender.Send(new GetAllBillboardsQuery(page, pageSize), ct);
        return Ok(result);
    }

    // ── POST /api/billboards — Admin: create ──────────────────────────────────
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [EnableRateLimiting("public-api")]
    [RequestSizeLimit(4096)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Create(
        [FromBody] CreateBillboardRequest request,
        CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await sender.Send(new CreateBillboardCommand(
            userId, request.Title, request.Body, request.Placement,
            request.StartsAt, request.EndsAt, request.CtaLabel, request.CtaUrl,
            request.Priority), ct);
        if (result.IsFailure)
            return UnprocessableEntity(new ProblemDetails { Detail = string.Join("; ", result.Errors), Status = 422 });
        return Created(string.Empty, result.Value);
    }

    // ── PUT /api/billboards/{id} — Admin: update ──────────────────────────────
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [EnableRateLimiting("public-api")]
    [RequestSizeLimit(4096)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateBillboardRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new UpdateBillboardCommand(
            id, request.Title, request.Body, request.CtaLabel, request.CtaUrl,
            request.StartsAt, request.EndsAt, request.Priority), ct);
        if (result.IsFailure)
            return NotFound(new ProblemDetails { Detail = string.Join("; ", result.Errors), Status = 404 });
        return Ok(result.Value);
    }

    // ── PATCH /api/billboards/{id}/status — Admin: activate/pause/expire ──────
    [HttpPatch("{id:guid}/status")]
    [Authorize(Roles = "Admin")]
    [EnableRateLimiting("public-api")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> SetStatus(Guid id, [FromBody] SetStatusRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new SetBillboardStatusCommand(id, request.Status), ct);
        if (result.IsFailure)
            return UnprocessableEntity(new ProblemDetails { Detail = string.Join("; ", result.Errors), Status = 422 });
        return Ok(result.Value);
    }

    // ── POST /api/billboards/{id}/image — Admin: upload image ─────────────────
    [HttpPost("{id:guid}/image")]
    [Authorize(Roles = "Admin")]
    [EnableRateLimiting("public-api")]
    [RequestSizeLimit(5_242_880)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> UploadImage(
        Guid id, [FromForm] IFormFile? image, CancellationToken ct)
    {
        if (image is null || image.Length == 0)
            return BadRequest(new ProblemDetails { Detail = "Se requiere una imagen.", Status = 400 });
        var allowed = new[] { "image/jpeg", "image/png", "image/webp" };
        if (!allowed.Contains(image.ContentType, StringComparer.OrdinalIgnoreCase))
            return BadRequest(new ProblemDetails { Detail = "Solo JPEG, PNG o WebP.", Status = 400 });

        using var ms = new MemoryStream();
        await image.CopyToAsync(ms, ct);
        var result = await sender.Send(new UploadBillboardImageCommand(id, ms.ToArray(), image.ContentType), ct);
        if (result.IsFailure)
            return UnprocessableEntity(new ProblemDetails { Detail = string.Join("; ", result.Errors), Status = 422 });
        return Ok(result.Value);
    }

    private bool TryGetUserId(out Guid userId)
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(raw, out userId);
    }
}

// ── Request models ────────────────────────────────────────────────────────────

public sealed record CreateBillboardRequest(
    string Title, string? Body, string Placement,
    DateTimeOffset StartsAt, DateTimeOffset EndsAt,
    string? CtaLabel, string? CtaUrl, int Priority = 0);

public sealed record UpdateBillboardRequest(
    string Title, string? Body, string? CtaLabel, string? CtaUrl,
    DateTimeOffset StartsAt, DateTimeOffset EndsAt, int Priority);

public sealed record SetStatusRequest(string Status);
