using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PawTrack.Application.Chat.Commands.OpenChatThread;
using PawTrack.Application.Chat.Commands.SendChatMessage;
using PawTrack.Application.Chat.Queries.GetChatMessages;
using PawTrack.Application.Chat.Queries.GetChatThreads;
using System.Security.Claims;

namespace PawTrack.API.Controllers;

/// <summary>
/// Manages masked chat threads between pet owners and finders.
/// All endpoints require authentication — neither party's contact details are stored
/// or transmitted through this channel.
/// </summary>
[ApiController]
[Route("api/chat")]
[Authorize]
public sealed class ChatController(ISender sender) : ControllerBase
{
    // ── POST /api/chat/threads — open (or retrieve) a thread ─────────────────

    /// <summary>Opens a masked chat thread linked to a lost-pet event.</summary>
    [HttpPost("threads")]
    [EnableRateLimiting("chat-message")]
    [RequestSizeLimit(4096)] // thread-open body is tiny; prevent large JSON payload
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> OpenThread(
        [FromBody] OpenThreadRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var result = await sender.Send(
            new OpenChatThreadCommand(request.LostPetEventId, userId),
            cancellationToken);

        if (result.IsFailure)
            return UnprocessableEntity(new ProblemDetails
            {
                Title  = "No se pudo abrir el hilo",
                Status = 422,
                Extensions = { ["errors"] = result.Errors },
            });

        return CreatedAtAction(nameof(GetMessages), new { threadId = result.Value }, new { threadId = result.Value });
    }

    // ── GET /api/chat/threads?lostPetEventId={id} — list threads for an event ─

    /// <summary>Returns all threads for a lost-pet event. Owner sees all; finder sees only their own.</summary>
    [HttpGet("threads")]
    [EnableRateLimiting("chat-message")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetThreads(
        [FromQuery] Guid lostPetEventId,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var result = await sender.Send(
            new GetChatThreadsQuery(lostPetEventId, userId),
            cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : Forbid();
    }

    // ── POST /api/chat/threads/{threadId}/messages ────────────────────────────

    /// <summary>Appends a masked message to an existing thread.</summary>
    [HttpPost("threads/{threadId:guid}/messages")]
    [EnableRateLimiting("chat-message")]
    [RequestSizeLimit(8192)] // message body ≤ 800 chars; 8 KB is ample, stops oversized JSON
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> SendMessage(
        Guid threadId,
        [FromBody] SendMessageRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var result = await sender.Send(
            new SendChatMessageCommand(threadId, userId, request.Body),
            cancellationToken);

        if (result.IsFailure)
            return UnprocessableEntity(new ProblemDetails
            {
                Title  = "No se pudo enviar el mensaje",
                Status = 422,
                Extensions = { ["errors"] = result.Errors },
            });

        return CreatedAtAction(nameof(GetMessages), new { threadId }, new { messageId = result.Value });
    }

    // ── GET /api/chat/threads/{threadId}/messages ─────────────────────────────

    /// <summary>Returns all messages in the thread and marks unread ones as read.</summary>
    [HttpGet("threads/{threadId:guid}/messages")]
    [EnableRateLimiting("chat-message")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMessages(
        Guid threadId,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var result = await sender.Send(
            new GetChatMessagesQuery(threadId, userId),
            cancellationToken);

        if (result.IsFailure)
        {
            return result.Errors.Contains("Acceso denegado.")
                ? Forbid()
                : NotFound(new ProblemDetails { Title = "Hilo no encontrado", Status = 404 });
        }

        return Ok(result.Value);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private bool TryGetUserId(out Guid userId)
    {
        userId = Guid.Empty;
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? User.FindFirstValue("sub");
        return Guid.TryParse(claim, out userId);
    }
}

// ── Request models ─────────────────────────────────────────────────────────────

/// <remarks>OwnerUserId is intentionally absent — the server resolves it from the database
/// to prevent BOLA (a forged owner-id spam/harassment attack).</remarks>
public sealed record OpenThreadRequest(Guid LostPetEventId);

public sealed record SendMessageRequest(string Body);
