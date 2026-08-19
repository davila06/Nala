using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PawTrack.Application.Stores;

namespace PawTrack.API.Controllers;

[ApiController]
[Route("api/admin/stores")]
[Authorize(Roles = "Admin")]
public sealed class AdminStoresController(ISender sender) : ControllerBase
{
    // ── GET /api/admin/stores/pending ─────────────────────────────────────────
    [HttpGet("pending")]
    [EnableRateLimiting("public-api")]
    public async Task<IActionResult> GetPending(CancellationToken ct)
    {
        var result = await sender.Send(new GetPendingStoresQuery(), ct);
        return result.IsSuccess ? Ok(result.Value) : Ok(Array.Empty<object>());
    }

    // ── GET /api/admin/stores ─────────────────────────────────────────────────
    [HttpGet]
    [EnableRateLimiting("public-api")]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var result = await sender.Send(new GetPublicStoresQuery(), ct);
        return result.IsSuccess ? Ok(result.Value) : Ok(Array.Empty<object>());
    }

    // ── PUT /api/admin/stores/{id}/review ─────────────────────────────────────
    [HttpPut("{storeId:guid}/review")]
    [EnableRateLimiting("public-api")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Review(
        Guid storeId,
        [FromBody] ReviewStoreRequest request,
        CancellationToken ct)
    {
        var result = await sender.Send(new ReviewStoreCommand(storeId, request.Approve), ct);
        if (result.IsFailure)
            return UnprocessableEntity(new ProblemDetails { Detail = string.Join("; ", result.Errors), Status = 422 });
        return NoContent();
    }
}

public sealed record ReviewStoreRequest(bool Approve);
