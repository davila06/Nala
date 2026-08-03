using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PawTrack.Application.Municipalities;
using PawTrack.Application.Municipalities.Commands.BulkUpdateStatus;
using PawTrack.Application.Municipalities.Commands.RecordCapture;
using PawTrack.Application.Municipalities.Commands.TransferCapture;
using PawTrack.Application.Municipalities.Commands.UpdateCaptureStatus;
using PawTrack.Application.Municipalities.Commands.UploadCapturePhoto;
using PawTrack.Application.Municipalities.Interfaces;
using PawTrack.Application.Municipalities.Queries.GetCantonStatistics;
using PawTrack.Application.Municipalities.Queries.GetRegionalDashboard;
using PawTrack.Application.Municipalities.Queries.SearchCaptures;
using PawTrack.Domain.Municipalities;
using System.Security.Claims;

namespace PawTrack.API.Controllers;

[ApiController]
[Route("api/municipalities")]
[Authorize(Roles = "Municipality,Admin")]
public sealed class MunicipalController(ISender sender, IMunicipalSubscriptionService subscriptionService)
    : ControllerBase
{
    // ── GET /api/municipalities/profile ───────────────────────────────────────
    [HttpGet("profile")]
    [EnableRateLimiting("public-api")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProfile(CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await sender.Send(new GetMunicipalProfileQuery(userId), ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Errors);
    }

    // ── GET /api/municipalities/captures ──────────────────────────────────────
    [HttpGet("captures")]
    [EnableRateLimiting("public-api")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Search(
        [FromQuery] string? canton,
        [FromQuery] CapturedAnimalStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await sender.Send(
            new SearchCapturesQuery(userId, canton, status, page, pageSize),
            cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Errors);
    }

    // ── POST /api/municipalities/captures ─────────────────────────────────────
    [HttpPost("captures")]
    [EnableRateLimiting("public-api")]
    [RequestSizeLimit(512)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Record(
        [FromBody] RecordCaptureRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var result = await sender.Send(new RecordCaptureCommand(
            userId,
            request.Canton,
            request.Species,
            request.Color,
            request.Breed,
            request.EstimatedAge,
            request.Notes,
            request.CollarChipNumber,
            request.CapturedAt), cancellationToken);

        if (result.IsFailure)
            return UnprocessableEntity(new ProblemDetails { Detail = string.Join(", ", result.Errors) });

        return Created($"api/municipalities/captures/{result.Value!.Id}", result.Value);
    }

    // ── PUT /api/municipalities/captures/{id}/status ──────────────────────────
    [HttpPut("captures/{id:guid}/status")]
    [EnableRateLimiting("public-api")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> UpdateStatus(
        Guid id,
        [FromBody] UpdateStatusRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new UpdateCaptureStatusCommand(id, request.Status, request.MatchedPetId),
            cancellationToken);

        if (result.IsFailure)
            return UnprocessableEntity(new ProblemDetails { Detail = string.Join(", ", result.Errors) });

        return Ok(result.Value);
    }

    // ── PUT /api/municipalities/captures/bulk-status — Full+ ──────────────────
    [HttpPut("captures/bulk-status")]
    [EnableRateLimiting("public-api")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status402PaymentRequired)]
    public async Task<IActionResult> BulkUpdateStatus(
        [FromBody] BulkUpdateStatusRequest request,
        CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        if (!await subscriptionService.IsFullOrAboveAsync(userId, ct))
            return StatusCode(402, new ProblemDetails { Detail = "La actualización masiva requiere Full o Red Regional.", Status = 402 });

        var result = await sender.Send(
            new BulkUpdateStatusCommand(userId, request.AnimalIds, request.NewStatus, request.MatchedPetId), ct);

        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Errors);
    }

    // ── POST /api/municipalities/captures/{id}/photo — Full+ ──────────────────
    [HttpPost("captures/{id:guid}/photo")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(5_242_880)]
    [EnableRateLimiting("public-api")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status402PaymentRequired)]
    public async Task<IActionResult> UploadPhoto(
        Guid id,
        IFormFile photo,
        CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        if (!await subscriptionService.IsFullOrAboveAsync(userId, ct))
            return StatusCode(402, new ProblemDetails { Detail = "La carga de fotos requiere Full o Red Regional.", Status = 402 });

        var allowed = new[] { "image/jpeg", "image/png" };
        if (!allowed.Contains(photo.ContentType, StringComparer.OrdinalIgnoreCase))
            return BadRequest(new ProblemDetails { Detail = "Solo se aceptan JPEG o PNG.", Status = 400 });

        using var ms = new MemoryStream();
        await photo.CopyToAsync(ms, ct);

        var result = await sender.Send(
            new UploadCapturePhotoCommand(userId, id, ms.ToArray(), photo.ContentType), ct);

        return result.IsSuccess ? Ok(new { photoUrl = result.Value }) : BadRequest(result.Errors);
    }

    // ── GET /api/municipalities/stats — Full+ ─────────────────────────────────
    [HttpGet("stats")]
    [EnableRateLimiting("public-api")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status402PaymentRequired)]
    public async Task<IActionResult> GetStats(
        [FromQuery] string? canton,
        CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        if (!await subscriptionService.IsFullOrAboveAsync(userId, ct))
            return StatusCode(402, new ProblemDetails { Detail = "Las estadísticas requieren Full o Red Regional.", Status = 402 });

        var result = await sender.Send(new GetCantonStatisticsQuery(userId, canton), ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Errors);
    }

    // ── GET /api/municipalities/regional — Red Regional only ──────────────────
    [HttpGet("regional")]
    [EnableRateLimiting("public-api")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status402PaymentRequired)]
    public async Task<IActionResult> GetRegionalDashboard(CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        if (!await subscriptionService.IsRedRegionalAsync(userId, ct))
            return StatusCode(402, new ProblemDetails { Detail = "El dashboard regional requiere Red Regional.", Status = 402 });

        var result = await sender.Send(new GetRegionalDashboardQuery(userId), ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Errors);
    }

    // ── POST /api/municipalities/captures/{id}/transfer — Red Regional ────────
    [HttpPost("captures/{id:guid}/transfer")]
    [EnableRateLimiting("public-api")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status402PaymentRequired)]
    public async Task<IActionResult> Transfer(
        Guid id,
        [FromBody] TransferRequest request,
        CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        if (!await subscriptionService.IsRedRegionalAsync(userId, ct))
            return StatusCode(402, new ProblemDetails { Detail = "Las transferencias requieren Red Regional.", Status = 402 });

        var result = await sender.Send(
            new TransferCaptureCommand(userId, id, request.DestinationCanton, request.Notes), ct);

        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Errors);
    }

    // ── Admin: POST /api/municipalities/admin/profiles ────────────────────────
    [HttpPost("admin/profiles")]
    [Authorize(Roles = "Admin")]
    [EnableRateLimiting("public-api")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> UpsertProfile(
        [FromBody] UpsertProfileRequest request,
        CancellationToken ct)
    {
        if (!Enum.TryParse<MunicipalTier>(request.Tier, ignoreCase: true, out var tier))
            return BadRequest(new ProblemDetails { Detail = $"Tier inválido: {request.Tier}" });

        var result = await sender.Send(new UpsertMunicipalProfileCommand(
            request.UserId, request.Canton, request.OrgName,
            tier, request.ExpiresAt, request.AdditionalCantons), ct);

        return result.IsSuccess ? Created(string.Empty, result.Value) : BadRequest(result.Errors);
    }

    private bool TryGetUserId(out Guid userId)
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(raw, out userId);
    }
}

// ── Request models ─────────────────────────────────────────────────────────────

public sealed record RecordCaptureRequest(
    string Canton, string Species, string Color,
    string? Breed, string? EstimatedAge, string? Notes,
    string? CollarChipNumber, DateTimeOffset? CapturedAt);

public sealed record UpdateStatusRequest(CapturedAnimalStatus Status, Guid? MatchedPetId);

public sealed record BulkUpdateStatusRequest(
    IReadOnlyList<Guid> AnimalIds,
    CapturedAnimalStatus NewStatus,
    Guid? MatchedPetId = null);

public sealed record TransferRequest(string DestinationCanton, string? Notes);

public sealed record UpsertProfileRequest(
    Guid UserId,
    string Canton,
    string OrgName,
    string Tier,
    DateTimeOffset? ExpiresAt,
    IEnumerable<string>? AdditionalCantons);
