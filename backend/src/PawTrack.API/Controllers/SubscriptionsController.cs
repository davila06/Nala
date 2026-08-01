using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PawTrack.Application.Subscriptions.Commands.ActivateSubscription;
using PawTrack.Application.Subscriptions.Commands.AdminActivateSubscription;
using PawTrack.Application.Subscriptions.Commands.CancelSubscription;
using PawTrack.Application.Subscriptions.Commands.CreateSubscription;
using PawTrack.Application.Subscriptions.Commands.ReportPayment;
using PawTrack.Application.Subscriptions.Queries.GetAdminSubscriptions;
using PawTrack.Application.Subscriptions.Queries.GetMySubscription;
using PawTrack.Domain.Subscriptions;
using System.Security.Claims;

namespace PawTrack.API.Controllers;

[ApiController]
[Route("api/subscriptions")]
[Authorize]
public sealed class SubscriptionsController(ISender sender) : ControllerBase
{
    // ── GET /api/subscriptions/me ────────────────────────────────────────────
    [HttpGet("me")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMine(
        [FromQuery] Guid? clinicId,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var result = await sender.Send(
            new GetMySubscriptionQuery(clinicId is null ? userId : null, clinicId),
            cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Errors);
    }

    // ── POST /api/subscriptions ──────────────────────────────────────────────
    [HttpPost]
    [EnableRateLimiting("public-api")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Create(
        [FromBody] CreateSubscriptionRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var result = await sender.Send(
            new CreateSubscriptionCommand(
                request.ClinicId is null ? userId : null,
                request.ClinicId,
                userId,
                request.Tier),
            cancellationToken);

        if (result.IsFailure)
            return UnprocessableEntity(new ProblemDetails { Detail = string.Join(", ", result.Errors) });

        return CreatedAtAction(nameof(GetMine), result.Value);
    }

    // ── PUT /api/subscriptions/activate ─────────────────────────────────────
    /// <summary>Called after the user confirms they sent the SINPE payment.</summary>
    [HttpPut("activate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Activate(
        [FromBody] ActivateSubscriptionRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new ActivateSubscriptionCommand(request.PaymentReference),
            cancellationToken);

        if (result.IsFailure)
            return UnprocessableEntity(new ProblemDetails { Detail = string.Join(", ", result.Errors) });

        return Ok(result.Value);
    }

    // ── DELETE /api/subscriptions/{id} ───────────────────────────────────────
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var result = await sender.Send(new CancelSubscriptionCommand(id, userId), cancellationToken);

        if (result.IsFailure)
            return UnprocessableEntity(new ProblemDetails { Detail = string.Join(", ", result.Errors) });

        return Ok(result.Value);
    }

    // ── PUT /api/subscriptions/{id}/report-payment — user confirms SINPE sent
    [HttpPut("{id:guid}/report-payment")]
    [EnableRateLimiting("public-api")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ReportPayment(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var result = await sender.Send(new ReportPaymentCommand(id, userId), cancellationToken);
        if (result.IsFailure)
            return UnprocessableEntity(new ProblemDetails { Detail = string.Join(", ", result.Errors) });
        return Ok(result.Value);
    }

    // ── GET /api/subscriptions/admin — Admin: list all / pending only ────────
    [HttpGet("admin")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> AdminGetAll(
        [FromQuery] bool pendingOnly = false,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(
            new GetAdminSubscriptionsQuery(pendingOnly, skip, take), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Errors);
    }

    // ── PUT /api/subscriptions/admin/{id}/activate — Admin: activate by ID ──
    [HttpPut("admin/{id:guid}/activate")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> AdminActivate(
        Guid id,
        [FromBody] AdminActivateRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new AdminActivateSubscriptionCommand(id, request.BillingMonths), cancellationToken);
        if (result.IsFailure)
            return UnprocessableEntity(new ProblemDetails { Detail = string.Join(", ", result.Errors) });
        return Ok(result.Value);
    }

    // ── DELETE /api/subscriptions/admin/{id} — Admin: cancel any subscription
    [HttpDelete("admin/{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> AdminCancel(Guid id, CancellationToken cancellationToken)
    {
        // Admin can cancel any subscription — pass a nil userId so ownership check is skipped
        var result = await sender.Send(new CancelSubscriptionCommand(id, Guid.Empty), cancellationToken);
        if (result.IsFailure)
            return UnprocessableEntity(new ProblemDetails { Detail = string.Join(", ", result.Errors) });
        return Ok(result.Value);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private bool TryGetUserId(out Guid userId)
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(raw, out userId);
    }
}

public sealed record CreateSubscriptionRequest(SubscriptionTier Tier, Guid? ClinicId);
public sealed record ActivateSubscriptionRequest(string PaymentReference);
public sealed record AdminActivateRequest(int BillingMonths = 1);
