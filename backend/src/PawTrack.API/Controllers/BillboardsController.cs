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
            request.Priority, request.AdvertiserName, request.Category, request.TargetCanton,
            request.ContractReference, request.BudgetCrc, request.FrequencyCapPerDay,
            request.IsCategoryExclusive, request.IsVip), ct);
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
            request.StartsAt, request.EndsAt, request.Priority, request.AdvertiserName,
            request.Category, request.TargetCanton, request.ContractReference,
            request.BudgetCrc, request.FrequencyCapPerDay, request.IsCategoryExclusive, request.IsVip), ct);
        if (result.IsFailure)
            return NotFound(new ProblemDetails { Detail = string.Join("; ", result.Errors), Status = 404 });
        return Ok(result.Value);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await sender.Send(new DeleteBillboardCommand(id, userId), ct);
        return result.IsSuccess ? NoContent() : NotFound(new ProblemDetails { Detail = string.Join("; ", result.Errors), Status = 404 });
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

    [HttpPost("{id:guid}/submit")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Submit(Guid id, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await sender.Send(new SubmitBillboardCampaignCommand(id, userId), ct);
        return result.IsSuccess ? Ok(result.Value) : UnprocessableEntity(new ProblemDetails { Detail = string.Join("; ", result.Errors), Status = 422 });
    }

    [HttpPost("{id:guid}/review")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Review(Guid id, [FromBody] ReviewBillboardRequest request, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await sender.Send(new ReviewBillboardCampaignCommand(id, userId, request.Approve, request.Note), ct);
        return result.IsSuccess ? Ok(result.Value) : UnprocessableEntity(new ProblemDetails { Detail = string.Join("; ", result.Errors), Status = 422 });
    }

    [HttpPost("{id:guid}/events")]
    [AllowAnonymous]
    [EnableRateLimiting("billboard-events")]
    public async Task<IActionResult> TrackDelivery(Guid id, [FromBody] BillboardDeliveryRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.EventKey) || request.EventKey.Length > 128 ||
            !Guid.TryParse(request.EventKey.Split(':')[0], out _) ||
            (request.Canton?.Length ?? 0) > 100 || request.Canton?.Any(char.IsControl) == true)
            return BadRequest(new ProblemDetails { Detail = "Evento de entrega inválido.", Status = 400 });

        var rawIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous";
        var visitorHash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(rawIp))).ToLowerInvariant();
        var hash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes($"{visitorHash}:{request.EventKey}"))).ToLowerInvariant();
        var result = await sender.Send(new TrackBillboardDeliveryCommand(id, request.EventType, hash, visitorHash, request.Canton), ct);
        return result.IsSuccess ? NoContent() : UnprocessableEntity(new ProblemDetails { Detail = string.Join("; ", result.Errors), Status = 422 });
    }

    [HttpGet("{id:guid}/metrics")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetMetrics(Guid id, [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, CancellationToken ct)
    {
        var end = to ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var start = from ?? end.AddDays(-30);
        if (start > end || start < end.AddDays(-366)) return BadRequest(new ProblemDetails { Detail = "Rango de fechas inválido.", Status = 400 });
        return Ok(await sender.Send(new GetBillboardCampaignMetricsQuery(id, start, end), ct));
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
    string? CtaLabel, string? CtaUrl, int Priority = 0,
    string AdvertiserName = "Pendiente de anunciante", string Category = "PetCare",
    string? TargetCanton = null, string? ContractReference = null,
    decimal BudgetCrc = 0, int FrequencyCapPerDay = 1, bool IsCategoryExclusive = false,
    bool IsVip = false);

public sealed record UpdateBillboardRequest(
    string Title, string? Body, string? CtaLabel, string? CtaUrl,
    DateTimeOffset StartsAt, DateTimeOffset EndsAt, int Priority,
    string AdvertiserName, string Category, string? TargetCanton,
    string? ContractReference, decimal BudgetCrc, int FrequencyCapPerDay,
    bool IsCategoryExclusive, bool IsVip);

public sealed record SetStatusRequest(string Status);
public sealed record ReviewBillboardRequest(bool Approve, string? Note);
public sealed record BillboardDeliveryRequest(string EventType, string EventKey, string? Canton);
