using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PawTrack.Application.Notifications.Commands.RespondResolveCheckNotification;
using PawTrack.Application.Notifications.Commands.MarkAllNotificationsRead;
using PawTrack.Application.Notifications.Commands.MarkNotificationRead;
using PawTrack.Application.Notifications.Commands.RegisterPushSubscription;
using PawTrack.Application.Notifications.Commands.UpdateNotificationPreferences;
using PawTrack.Application.Notifications.Queries.GetMyNotifications;
using PawTrack.Application.Notifications.Queries.GetNotificationPreferences;
using System.Security.Claims;

namespace PawTrack.API.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
public sealed class NotificationsController(ISender sender) : ControllerBase
{
    // ── GET /api/notifications ────────────────────────────────────────────────
    [HttpGet]
    [EnableRateLimiting("public-api")] // 30/min — handler fires 2 DB queries per call (paginated SELECT + COUNT)
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyNotifications(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        var result = await sender.Send(
            new GetMyNotificationsQuery(userId, page, pageSize), cancellationToken);

        return Ok(result.Value);
    }

    // ── PUT /api/notifications/{id}/read ─────────────────────────────────────
    [HttpPut("{id:guid}/read")]
    [EnableRateLimiting("notifications-write")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkAsRead(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        var result = await sender.Send(
            new MarkNotificationReadCommand(id, userId), cancellationToken);

        if (result.IsFailure)
        {
            return result.Errors.Contains("Access denied.")
                ? Forbid()
                : NotFound(new ProblemDetails { Title = "Notification not found", Status = 404 });
        }

        return NoContent();
    }

    // ── PUT /api/notifications/read-all ──────────────────────────────────────
    [HttpPut("read-all")]
    [EnableRateLimiting("notifications-write")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> MarkAllAsRead(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        await sender.Send(new MarkAllNotificationsReadCommand(userId), cancellationToken);
        return NoContent();
    }

    // ── POST /api/notifications/{id}/resolve-check-response ─────────────────
    [HttpPost("{id:guid}/resolve-check-response")]
    [EnableRateLimiting("notifications-write")]
    [RequestSizeLimit(128)] // body is ~22 bytes (single bool field); 128 caps all HTTP framing overhead
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> RespondResolveCheck(
        Guid id,
        [FromBody] ResolveCheckResponseRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        var result = await sender.Send(
            new RespondResolveCheckNotificationCommand(id, userId, request.FoundAtHome),
            cancellationToken);

        if (result.IsFailure)
        {
            if (result.Errors.Contains("Notification not found."))
                return NotFound(new ProblemDetails { Title = "Notification not found", Status = 404 });

            if (result.Errors.Contains("Access denied."))
                return Forbid();

            return UnprocessableEntity(new ProblemDetails
            {
                Title = "Could not process resolve-check response",
                Status = 422,
                Extensions = { ["errors"] = result.Errors },
            });
        }

        return NoContent();
    }

    // ── GET /api/notifications/preferences ───────────────────────────────────
    [HttpGet("preferences")]    [EnableRateLimiting("public-api")] // 30/min — read-side of preferences; sibling PUT already uses notifications-write    [ProducesResponseType(typeof(NotificationPreferencesDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPreferences(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        var result = await sender.Send(
            new GetNotificationPreferencesQuery(userId), cancellationToken);

        return Ok(result.Value);
    }

    // ── PUT /api/notifications/preferences ───────────────────────────────────
    [HttpPut("preferences")]
    [EnableRateLimiting("notifications-write")]
    [RequestSizeLimit(512)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdatePreferences(
        [FromBody] UpdateNotificationPreferencesRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        await sender.Send(
            new UpdateNotificationPreferencesCommand(userId, request.EnablePreventiveAlerts),
            cancellationToken);

        return NoContent();
    }

    // ── POST /api/notifications/push-subscription ─────────────────────────────
    [HttpPost("push-subscription")]
    [EnableRateLimiting("notifications-write")]
    [RequestSizeLimit(8192)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RegisterPushSubscription(
        [FromBody] RegisterPushSubscriptionRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        var result = await sender.Send(
            new RegisterPushSubscriptionCommand(userId, request.Endpoint, request.KeysJson),
            cancellationToken);

        if (result.IsFailure)
            return BadRequest(new ProblemDetails { Title = "Could not register push subscription", Status = 400 });

        return NoContent();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    private bool TryGetUserId(out Guid userId)
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? User.FindFirstValue("sub");
        return Guid.TryParse(claim, out userId);
    }
}

public sealed record ResolveCheckResponseRequest(bool FoundAtHome);
public sealed record UpdateNotificationPreferencesRequest(bool EnablePreventiveAlerts);
public sealed record RegisterPushSubscriptionRequest(string Endpoint, string KeysJson);
