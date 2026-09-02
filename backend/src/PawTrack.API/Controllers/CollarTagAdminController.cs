using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PawTrack.Application.Collars.Commands.Admin;
using PawTrack.Application.Collars.Queries.GetCollarAuditLogBySerial;
using PawTrack.Application.Collars.Queries.GetCollarTagMetrics;
using PawTrack.Domain.Collars;
using System.Text;

namespace PawTrack.API.Controllers;

[ApiController]
[Route("api/admin/collar-tags")]
[Authorize(Roles = "Admin")]
public sealed class CollarTagAdminController(ISender sender) : ControllerBase
{
    // ── GET /api/admin/collar-tags?skip=0&take=50&status=&soldAfter=&soldBefore=&serial= ─
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50,
        [FromQuery] string? serial = null,
        [FromQuery] CollarTagStatus? status = null,
        [FromQuery] DateTimeOffset? soldAfter = null,
        [FromQuery] DateTimeOffset? soldBefore = null,
        [FromServices] PawTrack.Application.Common.Interfaces.ICollarTagRepository repo = null!,
        CancellationToken cancellationToken = default)
    {
        var (items, total) = await repo.SearchAsync(serial, status, soldAfter, soldBefore, skip, take, cancellationToken);
        return Ok(new { total, items = items.Select(CollarTagDto.FromDomain) });
    }

    // ── GET /api/admin/collar-tags/metrics ────────────────────────────────────
    [HttpGet("metrics")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMetrics(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetCollarTagMetricsQuery(), cancellationToken);
        return Ok(result.Value);
    }

    // ── POST /api/admin/collar-tags — register single serial ─────────────────
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Register(
        [FromBody] RegisterCollarTagRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new RegisterCollarTagCommand(request.Serial, request.FirmwareVersion), cancellationToken);
        if (result.IsFailure)
            return UnprocessableEntity(new ProblemDetails { Detail = string.Join(", ", result.Errors) });
        return Created($"api/admin/collar-tags/{result.Value.Serial}", result.Value);
    }

    // ── POST /api/admin/collar-tags/{serial}/mark-sold ────────────────────────
    [HttpPost("{serial}/mark-sold")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> MarkSold(string serial, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new MarkCollarTagSoldCommand(serial), cancellationToken);
        if (result.IsFailure)
            return UnprocessableEntity(new ProblemDetails { Detail = string.Join(", ", result.Errors) });
        return NoContent();
    }

    // ── POST /api/admin/collar-tags/{serial}/revoke ───────────────────────────
    [HttpPost("{serial}/revoke")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Revoke(string serial, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new RevokeCollarCredentialCommand(serial), cancellationToken);
        if (result.IsFailure)
            return UnprocessableEntity(new ProblemDetails { Detail = string.Join(", ", result.Errors) });
        return NoContent();
    }

    // ── POST /api/admin/collar-tags/bulk-import — CSV upload ─────────────────
    [HttpPost("bulk-import")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> BulkImport(IFormFile file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new ProblemDetails { Detail = "CSV file is required." });

        var items = new List<(string Serial, string FirmwareVersion)>();
        using var reader = new System.IO.StreamReader(file.OpenReadStream(), Encoding.UTF8);

        // Expected CSV format: serial,firmwareVersion (no header row required; skip blank lines)
        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(line)) continue;
            var parts = line.Split(',');
            if (parts.Length < 2) continue;
            items.Add((parts[0].Trim(), parts[1].Trim()));
        }

        if (items.Count == 0)
            return BadRequest(new ProblemDetails { Detail = "No valid rows found in CSV." });

        var result = await sender.Send(new BulkImportCollarTagsCommand(items), cancellationToken);
        return Ok(result.Value);
    }

    // ── GET /api/admin/collar-tags/{serial}/audit-log ────────────────────────────────
    [HttpGet("{serial}/audit-log")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAuditLog(
        string serial,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetCollarAuditLogBySerialQuery(serial, skip, take), cancellationToken);
        return Ok(result.Value);
    }

    // ── POST /api/admin/collar-tags/bulk-mark-sold ────────────────────────
    [HttpPost("bulk-mark-sold")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> BulkMarkSold(
        [FromBody] BulkSerialsRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new BulkMarkCollarTagsSoldCommand(request.Serials), cancellationToken);
        return Ok(result.Value);
    }

    // ── POST /api/admin/collar-tags/bulk-revoke ──────────────────────────
    [HttpPost("bulk-revoke")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> BulkRevoke(
        [FromBody] BulkRevokeRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new BulkRevokeCollarTagsCommand(request.Serials, request.Reason), cancellationToken);
        return Ok(result.Value);
    }
}

public sealed record RegisterCollarTagRequest(string Serial, string FirmwareVersion);
public sealed record BulkSerialsRequest(IReadOnlyList<string> Serials);
public sealed record BulkRevokeRequest(IReadOnlyList<string> Serials, string? Reason);
