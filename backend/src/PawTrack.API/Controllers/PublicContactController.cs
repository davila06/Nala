using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PawTrack.Application.Notifications;

namespace PawTrack.API.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/public/contact")]
public sealed class PublicContactController(ISender sender) : ControllerBase
{
    [HttpPost]
    [EnableRateLimiting("public-contact")]
    [RequestSizeLimit(8_192)]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Submit([FromBody] SubmitContactMessageRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new SubmitContactMessageCommand(
            request.Name ?? string.Empty,
            request.Email ?? string.Empty,
            request.Topic ?? string.Empty,
            request.Message ?? string.Empty,
            request.Website ?? string.Empty), cancellationToken);

        if (result.IsSuccess)
            return Accepted(new { message = "El proveedor aceptó el mensaje para envío." });

        return StatusCode(StatusCodes.Status503ServiceUnavailable, new ProblemDetails
        {
            Status = StatusCodes.Status503ServiceUnavailable,
            Title = "No fue posible enviar el mensaje.",
            Detail = "Inténtalo de nuevo más tarde.",
        });
    }
}

/// <summary>Public contact message submitted by a visitor.</summary>
public sealed record SubmitContactMessageRequest(
    string? Name,
    string? Email,
    string? Topic,
    string? Message,
    string? Website);
