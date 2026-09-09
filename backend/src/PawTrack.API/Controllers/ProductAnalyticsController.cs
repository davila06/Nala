using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PawTrack.Application.ProductAnalytics;
using Asp.Versioning;

namespace PawTrack.API.Controllers;

[ApiController]
[Route("api/product-events")]
[ApiVersion("1.0")]
public sealed class ProductAnalyticsController(ISender sender) : ControllerBase
{
    [HttpPost]
    [AllowAnonymous]
    [EnableRateLimiting("public-api")]
    [RequestSizeLimit(16_384)]
    public async Task<IActionResult> Ingest(
        [FromBody] IngestProductEventRequest request,
        CancellationToken cancellationToken)
    {
        var userId = TryGetUserId();
        var result = await sender.Send(new IngestProductEventCommand(
            request.EventId,
            request.EventName,
            request.SchemaVersion,
            request.OccurredAt,
            request.AnonymousId,
            request.Source,
            userId,
            request.PetId,
            request.Canton,
            request.CorrelationId), cancellationToken);

        if (result.IsFailure)
            return UnprocessableEntity(new ProblemDetails
            {
                Title = "Invalid product event",
                Detail = string.Join("; ", result.Errors),
                Status = StatusCodes.Status422UnprocessableEntity,
            });

        return Accepted(new { accepted = result.Value });
    }

    [HttpGet("funnel")]
    [Authorize(Roles = "Admin")]
    [EnableRateLimiting("public-api")]
    public async Task<IActionResult> GetFunnel(
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        [FromQuery] string? canton,
        CancellationToken cancellationToken)
    {
        var end = to ?? DateTimeOffset.UtcNow;
        var start = from ?? end.AddDays(-30);
        if (start >= end || end - start > TimeSpan.FromDays(366))
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid date range",
                Detail = "The funnel range must be positive and no longer than 366 days.",
                Status = StatusCodes.Status400BadRequest,
            });

        var result = await sender.Send(new GetProductFunnelQuery(start, end, canton), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : Problem();
    }

    [HttpGet("funnel/export")]
    [Authorize(Roles = "Admin")]
    [EnableRateLimiting("public-api")]
    public async Task<IActionResult> ExportFunnel(
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        [FromQuery] string? canton,
        CancellationToken cancellationToken)
    {
        var end = to ?? DateTimeOffset.UtcNow;
        var start = from ?? end.AddDays(-30);
        if (start >= end || end - start > TimeSpan.FromDays(366))
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid date range",
                Detail = "The funnel range must be positive and no longer than 366 days.",
                Status = StatusCodes.Status400BadRequest,
            });

        var result = await sender.Send(new GetProductFunnelQuery(start, end, canton), cancellationToken);
        if (result.IsFailure) return Problem();

        var csv = new System.Text.StringBuilder("eventName,count,from,to,canton\n");
        foreach (var item in result.Value.Events.OrderBy(x => x.Key, StringComparer.Ordinal))
        {
            csv.Append(EscapeCsv(item.Key)).Append(',')
                .Append(item.Value).Append(',')
                .Append(result.Value.From.ToString("O")).Append(',')
                .Append(result.Value.To.ToString("O")).Append(',')
                .Append(EscapeCsv(result.Value.Canton)).Append('\n');
        }

        return File(
            System.Text.Encoding.UTF8.GetBytes(csv.ToString()),
            "text/csv; charset=utf-8",
            $"pawtrack-funnel-{start:yyyyMMdd}-{end:yyyyMMdd}.csv");
    }

    private static string EscapeCsv(string? value) =>
        string.IsNullOrEmpty(value) ? string.Empty : $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";

    private Guid? TryGetUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(claim, out var userId) ? userId : null;
    }
}

public sealed record IngestProductEventRequest(
    Guid EventId,
    string EventName,
    string SchemaVersion,
    DateTimeOffset OccurredAt,
    string AnonymousId,
    string Source,
    Guid? PetId,
    string? Canton,
    string? CorrelationId);