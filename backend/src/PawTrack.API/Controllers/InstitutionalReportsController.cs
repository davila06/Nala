using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PawTrack.Application.Regulatory.Commands;
using PawTrack.Application.Regulatory.Dtos;
using PawTrack.Application.Regulatory.Queries;
using PawTrack.Domain.Regulatory;
using System.Security.Claims;

namespace PawTrack.API.Controllers;

[ApiController]
[Route("api/institutional/reports")]
[Authorize(Roles = "Admin,Municipality,Clinic,Ally,Shelter")]
public sealed class InstitutionalReportsController(ISender sender, IConfiguration configuration) : ControllerBase
{
    [HttpGet("catalog")]
    [EnableRateLimiting("public-api")]
    public async Task<IActionResult> GetCatalog(
        [FromQuery] ExportScope scope = ExportScope.Institutional,
        CancellationToken cancellationToken = default)
    {
        if (!configuration.GetValue("Features:RegulatoryReportsEnabled", true)) return NotFound();
        if (scope is ExportScope.Admin or ExportScope.Nala && !User.IsInRole("Admin"))
            return Forbid();
        var result = await sender.Send(new GetReportCatalogQuery(scope), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Errors);
    }

    [HttpGet("preview")]
    [EnableRateLimiting("public-api")]
    public async Task<IActionResult> Preview(
        [FromQuery] ReportType reportType,
        [FromQuery] ExportScope scope,
        [FromQuery] DateOnly periodStart,
        [FromQuery] DateOnly periodEnd,
        [FromQuery] string? canton,
        [FromQuery] string? species,
        [FromQuery] string? status,
        [FromQuery] Guid? organizationId,
        CancellationToken cancellationToken)
    {
        if (!configuration.GetValue("Features:RegulatoryReportsEnabled", true)) return NotFound();
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await sender.Send(new GetReportPreviewQuery(
            userId,
            reportType,
            scope,
            periodStart,
            periodEnd,
            new RegulatoryReportFilter(canton, species, status, organizationId)), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : UnprocessableEntity(new ProblemDetails { Detail = string.Join("; ", result.Errors), Status = 422 });
    }

    [HttpPost("exports")]
    [EnableRateLimiting("public-api")]
    [RequestSizeLimit(2048)]
    public async Task<IActionResult> RequestExport(
        [FromBody] RequestRegulatoryExportRequest request,
        CancellationToken cancellationToken)
    {
        if (!configuration.GetValue("Features:RegulatoryReportsEnabled", true)) return NotFound();
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await sender.Send(new RequestRegulatoryExportCommand(
            userId,
            request.ReportType,
            request.Scope,
            request.Format,
            request.PeriodStart,
            request.PeriodEnd,
            request.Canton,
            request.OrganizationId,
            request.IdempotencyKey), cancellationToken);

        return result.IsSuccess
            ? Accepted($"/api/institutional/reports/exports/{result.Value!.Id}", result.Value)
            : UnprocessableEntity(new ProblemDetails { Detail = string.Join("; ", result.Errors), Status = 422 });
    }

    [HttpGet("exports")]
    [EnableRateLimiting("public-api")]
    public async Task<IActionResult> GetMyExports(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (!configuration.GetValue("Features:RegulatoryReportsEnabled", true)) return NotFound();
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await sender.Send(new GetMyRegulatoryExportsQuery(userId, page, pageSize), cancellationToken);
        return Ok(result.Value);
    }

    [HttpGet("exports/{id:guid}")]
    [EnableRateLimiting("public-api")]
    public async Task<IActionResult> GetExport(Guid id, CancellationToken cancellationToken)
    {
        if (!configuration.GetValue("Features:RegulatoryReportsEnabled", true)) return NotFound();
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await sender.Send(new GetRegulatoryExportStatusQuery(id, userId), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : NotFound();
    }

    [HttpPost("exports/{id:guid}/generate")]
    [EnableRateLimiting("public-api")]
    [RequestSizeLimit(512)]
    public async Task<IActionResult> GenerateExport(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await sender.Send(new GenerateRegulatoryExportCommand(id, userId), cancellationToken);
        return result.IsSuccess
            ? Ok(result.Value)
            : UnprocessableEntity(new ProblemDetails { Detail = string.Join("; ", result.Errors), Status = 422 });
    }

    [HttpGet("exports/{id:guid}/download")]
    [EnableRateLimiting("public-api")]
    public async Task<IActionResult> DownloadExport(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await sender.Send(new DownloadRegulatoryExportCommand(id, userId), cancellationToken);
        return result.IsSuccess
            ? File(result.Value!.Bytes, result.Value.ContentType, result.Value.FileName)
            : NotFound();
    }

    [HttpPost("exports/{id:guid}/regulatory-submission")]
    [EnableRateLimiting("public-api")]
    [RequestSizeLimit(2048)]
    public async Task<IActionResult> PrepareSubmission(
        Guid id,
        [FromBody] PrepareSubmissionRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await sender.Send(new PrepareRegulatorySubmissionCommand(
            id,
            userId,
            request.Destination,
            request.SubmissionType,
            request.IdempotencyKey), cancellationToken);
        return result.IsSuccess
            ? Accepted(result.Value)
            : UnprocessableEntity(new ProblemDetails { Detail = string.Join("; ", result.Errors), Status = 422 });
    }

    [HttpPost("submissions/{id:guid}/cancel")]
    [EnableRateLimiting("public-api")]
    public async Task<IActionResult> CancelSubmission(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await sender.Send(new CancelRegulatorySubmissionCommand(id, userId), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : UnprocessableEntity(result.Errors);
    }

    [HttpPost("submissions/{id:guid}/retry")]
    [EnableRateLimiting("public-api")]
    public async Task<IActionResult> RetrySubmission(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await sender.Send(new RetryRegulatorySubmissionCommand(id, userId), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : UnprocessableEntity(result.Errors);
    }

    private bool TryGetUserId(out Guid userId) =>
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);
}

public sealed record RequestRegulatoryExportRequest(
    ReportType ReportType,
    ExportScope Scope,
    ExportFormat Format,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    string? Canton,
    Guid? OrganizationId,
    string IdempotencyKey);

public sealed record PrepareSubmissionRequest(
    string Destination,
    string SubmissionType,
    string IdempotencyKey);
