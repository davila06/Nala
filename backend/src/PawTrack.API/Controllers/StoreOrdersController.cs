using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PawTrack.Application.Stores;
using PawTrack.Domain.Stores;
using System.Security.Claims;

namespace PawTrack.API.Controllers;

[ApiController]
[Route("api/store-orders")]
[Authorize]
public sealed class StoreOrdersController(ISender sender) : ControllerBase
{
    // ── POST /api/store-orders — customer places order ────────────────────────
    [HttpPost]
    [EnableRateLimiting("public-api")]
    [RequestSizeLimit(4096)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> PlaceOrder(
        [FromBody] PlaceOrderRequest request,
        CancellationToken ct)
    {
        if (!TryGetUserId(out var customerId)) return Unauthorized();
        if (!Enum.TryParse<OrderFulfillmentType>(request.FulfillmentType, ignoreCase: true, out var fulfillment))
            return BadRequest(new ProblemDetails { Detail = $"Tipo de entrega inválido: {request.FulfillmentType}.", Status = 400 });

        var lines = request.Lines
            .Select(l => new PlaceOrderLineInput(l.ProductId, l.Quantity))
            .ToList();

        var result = await sender.Send(new PlaceStoreOrderCommand(
            customerId, request.StoreId, fulfillment,
            request.DeliveryAddress, request.CustomerNote, lines, request.LocationId), ct);

        if (result.IsFailure)
            return UnprocessableEntity(new ProblemDetails { Detail = string.Join("; ", result.Errors), Status = 422 });
        return Created(string.Empty, result.Value);
    }

    // ── GET /api/store-orders/mine — customer's own orders ────────────────────
    [HttpGet("mine")]
    [EnableRateLimiting("public-api")]
    public async Task<IActionResult> GetMine(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        if (!TryGetUserId(out var customerId)) return Unauthorized();
        var result = await sender.Send(new GetMyStoreOrdersQuery(customerId, page, Math.Clamp(pageSize, 1, 50)), ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Errors);
    }

    // ── PUT /api/store-orders/{id}/report-payment ─────────────────────────────
    [HttpPut("{orderId:guid}/report-payment")]
    [EnableRateLimiting("public-api")]
    public async Task<IActionResult> ReportPayment(Guid orderId, CancellationToken ct)
    {
        if (!TryGetUserId(out var customerId)) return Unauthorized();
        var result = await sender.Send(new ReportStoreOrderPaymentCommand(customerId, orderId), ct);
        if (result.IsFailure)
            return UnprocessableEntity(new ProblemDetails { Detail = string.Join("; ", result.Errors), Status = 422 });
        return NoContent();
    }

    // ── GET /api/store-orders/incoming — store owner's orders ─────────────────
    [HttpGet("incoming")]
    [Authorize(Roles = "Store")]
    [EnableRateLimiting("public-api")]
    public async Task<IActionResult> GetIncoming(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await sender.Send(new GetStoreOrdersQuery(userId, page, pageSize), ct);
        return result.IsSuccess ? Ok(result.Value) : UnprocessableEntity(result.Errors);
    }

    // ── PUT /api/store-orders/{id}/confirm — store confirms after payment ──────
    [HttpPut("{orderId:guid}/confirm")]
    [Authorize(Roles = "Store")]
    [EnableRateLimiting("public-api")]
    public async Task<IActionResult> Confirm(
        Guid orderId,
        [FromBody] StoreOrderNoteRequest request,
        CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await sender.Send(new ConfirmStoreOrderCommand(userId, orderId, request.Note), ct);
        if (result.IsFailure)
            return UnprocessableEntity(new ProblemDetails { Detail = string.Join("; ", result.Errors), Status = 422 });
        return Ok(result.Value);
    }

    // ── PUT /api/store-orders/{id}/status — update delivery status ────────────
    [HttpPut("{orderId:guid}/status")]
    [Authorize(Roles = "Store")]
    [EnableRateLimiting("public-api")]
    public async Task<IActionResult> UpdateStatus(
        Guid orderId,
        [FromBody] UpdateOrderStatusRequest request,
        CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        if (!Enum.TryParse<StoreOrderStatus>(request.Status, ignoreCase: true, out var status))
            return BadRequest(new ProblemDetails { Detail = $"Estado inválido: {request.Status}.", Status = 400 });

        var result = await sender.Send(new UpdateStoreOrderStatusCommand(userId, orderId, status, request.Note), ct);
        if (result.IsFailure)
            return UnprocessableEntity(new ProblemDetails { Detail = string.Join("; ", result.Errors), Status = 422 });
        return Ok(result.Value);
    }

    private bool TryGetUserId(out Guid userId)
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(claim, out userId);
    }
}

// ── Request models ────────────────────────────────────────────────────────────

public sealed record PlaceOrderRequest(
    Guid StoreId,
    string FulfillmentType,
    string? DeliveryAddress,
    string? CustomerNote,
    IReadOnlyList<OrderLineRequest> Lines,
    Guid? LocationId = null);

public sealed record OrderLineRequest(Guid ProductId, int Quantity);
public sealed record StoreOrderNoteRequest(string? Note);
public sealed record UpdateOrderStatusRequest(string Status, string? Note);
