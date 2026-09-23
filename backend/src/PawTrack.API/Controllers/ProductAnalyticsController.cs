using System.Security.Claims;
using System.Diagnostics;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PawTrack.Application.ProductAnalytics;
using PawTrack.Infrastructure.Observability;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Audit;
using Asp.Versioning;

namespace PawTrack.API.Controllers;

[ApiController]
[Route("api/product-events")]
[Route("api/v1/product-events")]
[ApiVersion("1.0")]
public sealed class ProductAnalyticsController(
    ISender sender,
    IAuditLogRepository auditLogRepository,
    IUnitOfWork unitOfWork) : ControllerBase
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
            request.CorrelationId,
            TryGetTenantId(),
            TryGetTenantType()), cancellationToken);

        if (result.IsFailure)
            return UnprocessableEntity(new ProblemDetails
            {
                Title = "Invalid product event",
                Detail = string.Join("; ", result.Errors),
                Status = StatusCodes.Status422UnprocessableEntity,
            });

        if (result.Value)
            EnterpriseMetrics.ProductEventsIngested.Add(1);

        return Accepted(new { accepted = result.Value });
    }

    [HttpGet("funnel")]
    [Authorize(Roles = "Admin")]
    [EnableRateLimiting("public-api")]
    public async Task<IActionResult> GetFunnel(
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        [FromQuery] string? canton,
        [FromQuery] Guid? shelterId,
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

        var result = await sender.Send(
            new GetProductFunnelQuery(start, end, canton),
            cancellationToken);
        if (result.IsSuccess)
            EnterpriseMetrics.ProductFunnelQueries.Add(1);

        return result.IsSuccess ? Ok(result.Value) : Problem();
    }

    [HttpGet("funnel/export")]
    [Authorize(Roles = "Admin")]
    [EnableRateLimiting("public-api")]
    public async Task<IActionResult> ExportFunnel(
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        [FromQuery] string? canton,
        [FromQuery] Guid? shelterId,
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

        var result = await sender.Send(
            new GetProductFunnelQuery(start, end, canton),
            cancellationToken);
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

    [HttpGet("performance")]
    [Authorize(Roles = "Admin")]
    [EnableRateLimiting("public-api")]
    public async Task<IActionResult> GetPerformance(
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        [FromQuery] string? canton,
        [FromQuery] string? channel,
        [FromQuery] string? species,
        CancellationToken cancellationToken)
    {
        var end = to ?? DateTimeOffset.UtcNow;
        var start = from ?? end.AddDays(-90);
        if (start >= end || end - start > TimeSpan.FromDays(366))
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid date range",
                Detail = "The performance range must be positive and no longer than 366 days.",
                Status = StatusCodes.Status400BadRequest,
            });

        var result = await sender.Send(
            new GetProductPerformanceQuery(start, end, canton, channel, species),
            cancellationToken);
        if (result.IsSuccess)
        {
            EnterpriseMetrics.NorthStarActiveProtectedPets.Record(
                result.Value!.ActiveProtectedPets30Days,
                new TagList { { "window", "30d" } });
            EnterpriseMetrics.NorthStarActiveProtectedPets.Record(
                result.Value.ActiveProtectedPets90Days,
                new TagList { { "window", "90d" } });
            EnterpriseMetrics.NorthStarActiveProtectedPets.Record(
                result.Value.ActiveProtectedPets180Days,
                new TagList { { "window", "180d" } });
        }

        return result.IsSuccess ? Ok(result.Value) : Problem();
    }

    [HttpGet("performance/export")]
    [Authorize]
    [EnableRateLimiting("public-api")]
    public async Task<IActionResult> ExportPartnerPerformance(
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        [FromQuery] string? canton,
        [FromQuery] string? channel,
        [FromQuery] string? species,
        CancellationToken cancellationToken)
    {
        var keyId = User.FindFirstValue("ClinicApiKeyId");
        if (!Guid.TryParse(keyId, out _) || !TryGetTenantId().HasValue)
            return Forbid();

        var actorId = TryGetUserId();
        var tenantId = TryGetTenantId()!.Value;
        if (!actorId.HasValue)
            return Forbid();

        var end = to ?? DateTimeOffset.UtcNow;
        var start = from ?? end.AddDays(-90);
        if (start >= end || end - start > TimeSpan.FromDays(366))
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid date range",
                Detail = "The performance range must be positive and no longer than 366 days.",
                Status = StatusCodes.Status400BadRequest,
            });

        var monthStart = new DateTimeOffset(end.Year, end.Month, 1, 0, 0, 0, TimeSpan.Zero);
        if (await auditLogRepository.CountByActionSinceAsync(
                AuditAction.PartnerAnalyticsExported, actorId.Value, monthStart, cancellationToken) >= 20)
            return StatusCode(StatusCodes.Status429TooManyRequests, new ProblemDetails
            {
                Title = "Analytics export quota exceeded",
                Detail = "This Partner tenant has reached its monthly analytics export quota.",
                Status = StatusCodes.Status429TooManyRequests,
            });

        var result = await sender.Send(
            new GetProductPerformanceQuery(start, end, canton, channel, species, tenantId, "Clinic"),
            cancellationToken);
        if (result.IsFailure)
            return Problem();

        var csv = new System.Text.StringBuilder(
            "cohort,canton,channel,species,registeredPets,activatedPets,lostReports,reunitedReports,recoveryRatePercent,medianFirstResponseMinutes,medianReunionMinutes,p90FirstResponseMinutes,p90ReunionMinutes\n");
        foreach (var row in result.Value!.Cohorts)
        {
            csv.Append(EscapeCsv(row.Cohort)).Append(',')
                .Append(EscapeCsv(row.Canton)).Append(',')
                .Append(EscapeCsv(row.Channel)).Append(',')
                .Append(EscapeCsv(row.Species)).Append(',')
                .Append(row.RegisteredPets).Append(',')
                .Append(row.ActivatedPets).Append(',')
                .Append(row.LostReports).Append(',')
                .Append(row.ReunitedReports).Append(',')
                .Append(row.RecoveryRatePercent).Append(',')
                .Append(row.MedianFirstResponseMinutes).Append(',')
                .Append(row.MedianReunionMinutes).Append(',')
                .Append(row.P90FirstResponseMinutes).Append(',')
                .Append(row.P90ReunionMinutes).Append('\n');
        }

        await auditLogRepository.AddAsync(
            AuditLogEntry.Create(actorId.Value, AuditAction.PartnerAnalyticsExported,
                "PartnerAnalytics", tenantId.ToString(), $"range={start:O}/{end:O};key={keyId}"),
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return File(
            System.Text.Encoding.UTF8.GetBytes(csv.ToString()),
            "text/csv; charset=utf-8",
            $"pawtrack-partner-performance-{start:yyyyMMdd}-{end:yyyyMMdd}.csv");
    }

    private static string EscapeCsv(string? value) =>
        string.IsNullOrEmpty(value) ? string.Empty : $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";

    private Guid? TryGetUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(claim, out var userId) ? userId : null;
    }

    private Guid? TryGetTenantId()
    {
        var claim = User.FindFirstValue("ClinicId");
        return Guid.TryParse(claim, out var tenantId) ? tenantId : null;
    }

    private string? TryGetTenantType() =>
        User.FindFirstValue("ClinicId") is null ? null : "Clinic";
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
