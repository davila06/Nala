using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PawTrack.Application.Medical;
using PawTrack.Domain.Medical;
using System.Security.Claims;

namespace PawTrack.API.Controllers;

[ApiController]
[Route("api/pets/{petId:guid}/medical")]
[Authorize]
public sealed class MedicalController(ISender sender) : ControllerBase
{
    // ── GET /api/me/medical/reminders — aggregate across all pets ────────────
    [HttpGet("/api/me/medical/reminders")]
    [EnableRateLimiting("public-api")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyReminders(
        [FromQuery] int daysAhead = 30, CancellationToken ct = default)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await sender.Send(new GetMyRemindersQuery(userId, daysAhead), ct);
        if (result.IsFailure)
            return UnprocessableEntity(new ProblemDetails { Detail = string.Join("; ", result.Errors), Status = 422 });
        return Ok(result.Value);
    }

    // ── GET /api/pets/{petId}/medical/access-log — audit log for owner ───────
    [HttpGet("access-log")]
    [EnableRateLimiting("public-api")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAccessLog(
        Guid petId, [FromQuery] int limit = 50, CancellationToken ct = default)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await sender.Send(new GetClinicAccessLogQuery(petId, userId, limit), ct);
        if (result.IsFailure)
            return UnprocessableEntity(new ProblemDetails { Detail = string.Join("; ", result.Errors), Status = 422 });
        return Ok(result.Value);
    }

    // ── GET /api/pets/{petId}/medical/count — no plan gate, teaser for non-Familia ──
    [HttpGet("count")]
    [EnableRateLimiting("public-api")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCount(Guid petId, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await sender.Send(new GetMedicalRecordCountQuery(petId, userId), ct);
        if (result.IsFailure)
            return UnprocessableEntity(new ProblemDetails { Detail = string.Join("; ", result.Errors), Status = 422 });
        return Ok(result.Value);
    }

    // ── GET /api/pets/{petId}/medical ─────────────────────────────────────────
    [HttpGet]
    [EnableRateLimiting("public-api")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> GetHistory(Guid petId, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await sender.Send(new GetMedicalHistoryQuery(petId, userId), ct);
        if (result.IsFailure)
            return UnprocessableEntity(new ProblemDetails { Detail = string.Join("; ", result.Errors), Status = 422 });
        return Ok(result.Value);
    }

    // ── POST /api/pets/{petId}/medical ────────────────────────────────────────
    [HttpPost]
    [Consumes("multipart/form-data")]
    [EnableRateLimiting("public-api")]
    [RequestSizeLimit(5_242_880)] // 5MB — PDF/photo documents
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> AddRecord(
        Guid petId,
        [FromForm] AddMedicalRecordRequest request,
        CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        byte[]? docBytes = null;
        string? docContentType = null;
        if (request.Document is { Length: > 0 })
        {
            var allowed = new[] { "application/pdf", "image/jpeg", "image/png" };
            if (!allowed.Contains(request.Document.ContentType, StringComparer.OrdinalIgnoreCase))
                return BadRequest(new ProblemDetails { Detail = "Solo se aceptan PDF, JPEG o PNG.", Status = 400 });
            using var ms = new MemoryStream();
            await request.Document.CopyToAsync(ms, ct);
            docBytes = ms.ToArray();
            docContentType = request.Document.ContentType;
        }

        if (!Enum.TryParse<MedicalRecordType>(request.Type, ignoreCase: true, out var recordType))
            return BadRequest(new ProblemDetails { Detail = $"Tipo inválido: {request.Type}.", Status = 400 });

        var result = await sender.Send(new AddMedicalRecordCommand(
            petId, userId, recordType,
            request.Date, request.Description,
            request.VetName, request.ClinicName,
            request.NextDueDate,
            docBytes, docContentType,
            request.WeightKg, request.DosageDescription,
            request.Frequency, request.DurationDays, request.MedicationEndDate), ct);

        if (result.IsFailure)
            return UnprocessableEntity(new ProblemDetails { Detail = string.Join("; ", result.Errors), Status = 422 });

        return Created(string.Empty, result.Value);
    }

    // ── GET /api/pets/{petId}/medical/reminders ───────────────────────────────
    [HttpGet("reminders")]
    [EnableRateLimiting("public-api")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetReminders(Guid petId, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await sender.Send(new GetVetRemindersQuery(petId, userId), ct);
        if (result.IsFailure)
            return UnprocessableEntity(new ProblemDetails { Detail = string.Join("; ", result.Errors), Status = 422 });
        return Ok(result.Value);
    }

    // ── PUT /api/pets/{petId}/medical/reminders/{id}/complete ─────────────────
    [HttpPut("reminders/{reminderId:guid}/complete")]
    [EnableRateLimiting("public-api")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> CompleteReminder(Guid petId, Guid reminderId, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await sender.Send(new CompleteVetReminderCommand(reminderId, userId), ct);
        if (result.IsFailure)
            return UnprocessableEntity(new ProblemDetails { Detail = string.Join("; ", result.Errors), Status = 422 });
        return NoContent();
    }

    // ── GET /api/pets/{petId}/medical/export ──────────────────────────────────
    [HttpGet("export")]
    [EnableRateLimiting("public-api")]
    [Produces("application/pdf")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ExportPdf(Guid petId, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await sender.Send(new ExportMedicalHistoryCommand(petId, userId), ct);
        if (result.IsFailure)
            return UnprocessableEntity(new ProblemDetails { Detail = string.Join("; ", result.Errors), Status = 422 });
        return File(result.Value!, "application/pdf", $"historial-{petId}.pdf");
    }

    // ── DELETE /api/pets/{petId}/medical/{recordId} ───────────────────────────
    [HttpDelete("{recordId:guid}")]
    [EnableRateLimiting("public-api")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> DeleteRecord(Guid petId, Guid recordId, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await sender.Send(new DeleteMedicalRecordCommand(recordId, userId), ct);
        if (result.IsFailure)
            return UnprocessableEntity(new ProblemDetails { Detail = string.Join("; ", result.Errors), Status = 422 });
        return NoContent();
    }

    // ── PUT /api/pets/{petId}/medical/{recordId} ──────────────────────────────
    [HttpPut("{recordId:guid}")]
    [EnableRateLimiting("public-api")]
    [RequestSizeLimit(4096)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> UpdateRecord(
        Guid petId,
        Guid recordId,
        [FromBody] UpdateMedicalRecordRequest request,
        CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        if (!Enum.TryParse<MedicalRecordType>(request.Type, ignoreCase: true, out var recordType))
            return BadRequest(new ProblemDetails { Detail = $"Tipo inválido: {request.Type}.", Status = 400 });

        var result = await sender.Send(new UpdateMedicalRecordCommand(
            recordId, userId, recordType,
            request.Date, request.Description,
            request.VetName, request.ClinicName, request.NextDueDate,
            request.WeightKg, request.DosageDescription,
            request.Frequency, request.DurationDays, request.MedicationEndDate), ct);

        if (result.IsFailure)
            return UnprocessableEntity(new ProblemDetails { Detail = string.Join("; ", result.Errors), Status = 422 });
        return Ok(result.Value);
    }

    // ── POST /api/pets/{petId}/medical/reminders ──────────────────────────────
    [HttpPost("reminders")]
    [EnableRateLimiting("public-api")]
    [RequestSizeLimit(2048)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> CreateReminder(
        Guid petId,
        [FromBody] CreateVetReminderRequest request,
        CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        if (!Enum.TryParse<MedicalRecordType>(request.Type, ignoreCase: true, out var reminderType))
            return BadRequest(new ProblemDetails { Detail = $"Tipo inválido: {request.Type}.", Status = 400 });

        var result = await sender.Send(new CreateVetReminderCommand(
            petId, userId, reminderType,
            request.DueDate, request.Title, request.Notes), ct);

        if (result.IsFailure)
            return UnprocessableEntity(new ProblemDetails { Detail = string.Join("; ", result.Errors), Status = 422 });
        return Created(string.Empty, result.Value);
    }

    // ── DELETE /api/pets/{petId}/medical/reminders/{reminderId} ──────────────
    [HttpDelete("reminders/{reminderId:guid}")]
    [EnableRateLimiting("public-api")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> DeleteReminder(Guid petId, Guid reminderId, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await sender.Send(new DeleteVetReminderCommand(reminderId, userId), ct);
        if (result.IsFailure)
            return UnprocessableEntity(new ProblemDetails { Detail = string.Join("; ", result.Errors), Status = 422 });
        return NoContent();
    }

    private bool TryGetUserId(out Guid userId)
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(raw, out userId);
    }
}

public sealed record AddMedicalRecordRequest(
    string Type,
    DateOnly Date,
    string Description,
    string? VetName,
    string? ClinicName,
    DateOnly? NextDueDate,
    IFormFile? Document,
    decimal? WeightKg,
    string? DosageDescription,
    string? Frequency,
    int? DurationDays,
    DateOnly? MedicationEndDate);

public sealed record UpdateMedicalRecordRequest(
    string Type,
    DateOnly Date,
    string Description,
    string? VetName,
    string? ClinicName,
    DateOnly? NextDueDate,
    decimal? WeightKg,
    string? DosageDescription,
    string? Frequency,
    int? DurationDays,
    DateOnly? MedicationEndDate);

public sealed record CreateVetReminderRequest(
    string Type,
    DateOnly DueDate,
    string Title,
    string? Notes);
