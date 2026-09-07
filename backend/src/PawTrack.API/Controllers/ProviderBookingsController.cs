using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PawTrack.Application.ServiceProviders;
using PawTrack.Domain.ServiceProviders;
using System.Security.Claims;

namespace PawTrack.API.Controllers;

[ApiController]
[Route("api/provider-bookings")]
[Authorize]
[EnableRateLimiting("public-api")]
public sealed class ProviderBookingsController(ISender sender) : ControllerBase
{
    [HttpGet("availability/{providerServiceId:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetAvailability(
        Guid providerServiceId,
        [FromQuery] DateOnly date,
        CancellationToken ct)
    {
        var result = await sender.Send(new GetAvailableProviderServiceSlotsQuery(providerServiceId, date), ct);
        return result.IsSuccess
            ? Ok(result.Value)
            : NotFound(new ProblemDetails { Title = "Servicio no disponible", Status = StatusCodes.Status404NotFound });
    }

    [HttpPost]
    [RequestSizeLimit(2048)]
    public async Task<IActionResult> Create([FromBody] CreateProviderBookingRequest request, CancellationToken ct)
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (!Guid.TryParse(claim, out var customerUserId)) return Unauthorized();

        var result = await sender.Send(new CreateProviderBookingCommand(
            customerUserId, request.ProviderServiceId, request.PetId, request.StartsAt,
            request.Quantity, request.CustomerNote), ct);
        return result.IsSuccess
            ? Created(string.Empty, result.Value)
            : UnprocessableEntity(new ProblemDetails { Detail = string.Join("; ", result.Errors), Status = 422 });
    }

    [HttpGet("mine")]
    public async Task<IActionResult> GetMine([FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await sender.Send(new GetMyProviderBookingsQuery(userId, page, pageSize), ct);
        return result.IsSuccess ? Ok(result.Value) : Ok(Array.Empty<object>());
    }

    [HttpGet("incoming")]
    [Authorize(Roles = "ServiceProvider")]
    public async Task<IActionResult> GetIncoming([FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await sender.Send(new GetIncomingProviderBookingsQuery(userId, page, pageSize), ct);
        return result.IsSuccess ? Ok(result.Value) : UnprocessableEntity(result.Errors);
    }

    [HttpPut("{bookingId:guid}/status")]
    public async Task<IActionResult> UpdateStatus(Guid bookingId, [FromBody] UpdateProviderBookingStatusRequest request, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        if (!Enum.TryParse<ProviderBookingStatus>(request.Status, true, out var targetStatus))
            return BadRequest(new ProblemDetails { Detail = "Estado invalido.", Status = StatusCodes.Status400BadRequest });

        var result = await sender.Send(new UpdateProviderBookingStatusCommand(userId, bookingId, targetStatus, request.Reason), ct);
        return result.IsSuccess ? Ok(result.Value) : UnprocessableEntity(new ProblemDetails { Detail = string.Join("; ", result.Errors), Status = 422 });
    }

    [HttpPut("{bookingId:guid}/reschedule")]
    [RequestSizeLimit(512)]
    public async Task<IActionResult> Reschedule(Guid bookingId, [FromBody] RescheduleProviderBookingRequest request, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await sender.Send(new RescheduleProviderBookingCommand(userId, bookingId, request.StartsAt), ct);
        return result.IsSuccess
            ? Ok(result.Value)
            : UnprocessableEntity(new ProblemDetails { Detail = string.Join("; ", result.Errors), Status = 422 });
    }

    private bool TryGetUserId(out Guid userId)
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(claim, out userId);
    }
}

public sealed record CreateProviderBookingRequest(
    Guid ProviderServiceId,
    Guid PetId,
    DateTimeOffset StartsAt,
    int Quantity,
    string? CustomerNote);

public sealed record UpdateProviderBookingStatusRequest(string Status, string? Reason);
public sealed record RescheduleProviderBookingRequest(DateTimeOffset StartsAt);