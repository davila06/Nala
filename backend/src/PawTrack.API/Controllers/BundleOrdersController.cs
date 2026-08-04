using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PawTrack.Application.Bundles;
using PawTrack.Domain.Bundles;
using System.Security.Claims;

namespace PawTrack.API.Controllers;

[ApiController]
[Route("api/bundles")]
[Authorize]
public sealed class BundleOrdersController(ISender sender) : ControllerBase
{
    // ── POST /api/bundles ─────────────────────────────────────────────────────
    [HttpPost]
    [EnableRateLimiting("public-api")]
    [RequestSizeLimit(1024)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Create(
        [FromBody] CreateBundleOrderRequest request,
        CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        if (!Enum.TryParse<CollarModel>(request.CollarModel, ignoreCase: true, out var collarModel))
            return BadRequest(new ProblemDetails { Detail = $"Modelo de collar inválido: {request.CollarModel}." });

        var result = await sender.Send(new CreateBundleOrderCommand(
            userId, collarModel,
            request.ShippingFullName, request.ShippingAddress,
            request.ShippingCanton, request.ShippingPhone,
            request.DeliveryNotes), ct);

        if (result.IsFailure)
            return UnprocessableEntity(new ProblemDetails { Detail = string.Join("; ", result.Errors), Status = 422 });

        return Created(string.Empty, result.Value);
    }

    // ── GET /api/bundles/mine ─────────────────────────────────────────────────
    [HttpGet("mine")]
    [EnableRateLimiting("public-api")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMine(CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await sender.Send(new GetMyBundleOrdersQuery(userId), ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Errors);
    }

    // ── PUT /api/bundles/{id}/report-payment ──────────────────────────────────
    [HttpPut("{id:guid}/report-payment")]
    [EnableRateLimiting("public-api")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ReportPayment(Guid id, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await sender.Send(new ReportBundlePaymentCommand(id, userId), ct);

        if (result.IsFailure)
            return UnprocessableEntity(new ProblemDetails { Detail = string.Join("; ", result.Errors), Status = 422 });

        return NoContent();
    }

    // ── PUT /api/bundles/{id}/cancel ──────────────────────────────────────────
    [HttpPut("{id:guid}/cancel")]
    [EnableRateLimiting("public-api")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await sender.Send(new CancelBundleOrderCommand(id, userId, false, null), ct);

        if (result.IsFailure)
            return UnprocessableEntity(new ProblemDetails { Detail = string.Join("; ", result.Errors), Status = 422 });

        return Ok(result.Value);
    }

    // ── Admin endpoints ───────────────────────────────────────────────────────

    [HttpGet("admin")]
    [Authorize(Roles = "Admin")]
    [EnableRateLimiting("public-api")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken ct = default)
    {
        BundleOrderStatus? statusFilter = null;
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<BundleOrderStatus>(status, ignoreCase: true, out var s))
            statusFilter = s;

        var result = await sender.Send(new GetAllBundleOrdersQuery(statusFilter, page, pageSize), ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Errors);
    }

    [HttpPut("admin/{id:guid}/confirm-payment")]
    [Authorize(Roles = "Admin")]
    [EnableRateLimiting("public-api")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ConfirmPayment(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new ConfirmBundlePaymentCommand(id), ct);
        if (result.IsFailure)
            return UnprocessableEntity(new ProblemDetails { Detail = string.Join("; ", result.Errors), Status = 422 });
        return Ok(result.Value);
    }

    [HttpPut("admin/{id:guid}/sourced")]
    [Authorize(Roles = "Admin")]
    [EnableRateLimiting("public-api")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> MarkSourced(
        Guid id,
        [FromBody] AdminNotesRequest request,
        CancellationToken ct)
    {
        var result = await sender.Send(new MarkBundleSourcedCommand(id, request.AdminNotes), ct);
        if (result.IsFailure)
            return UnprocessableEntity(new ProblemDetails { Detail = string.Join("; ", result.Errors), Status = 422 });
        return Ok(result.Value);
    }

    [HttpPut("admin/{id:guid}/shipped")]
    [Authorize(Roles = "Admin")]
    [EnableRateLimiting("public-api")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> MarkShipped(
        Guid id,
        [FromBody] MarkShippedRequest request,
        CancellationToken ct)
    {
        var result = await sender.Send(new MarkBundleShippedCommand(id, request.TrackingNumber, request.AdminNotes), ct);
        if (result.IsFailure)
            return UnprocessableEntity(new ProblemDetails { Detail = string.Join("; ", result.Errors), Status = 422 });
        return Ok(result.Value);
    }

    [HttpPut("admin/{id:guid}/delivered")]
    [Authorize(Roles = "Admin")]
    [EnableRateLimiting("public-api")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> MarkDelivered(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new MarkBundleDeliveredCommand(id), ct);
        if (result.IsFailure)
            return UnprocessableEntity(new ProblemDetails { Detail = string.Join("; ", result.Errors), Status = 422 });
        return Ok(result.Value);
    }

    [HttpPut("admin/{id:guid}/cancel")]
    [Authorize(Roles = "Admin")]
    [EnableRateLimiting("public-api")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> AdminCancel(
        Guid id,
        [FromBody] AdminNotesRequest request,
        CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await sender.Send(new CancelBundleOrderCommand(id, userId, true, request.AdminNotes), ct);
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

// ── Request models ─────────────────────────────────────────────────────────────

public sealed record CreateBundleOrderRequest(
    string CollarModel,
    string ShippingFullName,
    string ShippingAddress,
    string ShippingCanton,
    string ShippingPhone,
    string? DeliveryNotes);

public sealed record MarkShippedRequest(string TrackingNumber, string? AdminNotes);
public sealed record AdminNotesRequest(string? AdminNotes);
