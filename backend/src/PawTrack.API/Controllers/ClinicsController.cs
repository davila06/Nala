using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PawTrack.Application.Clinics.Commands.AddClinicMedicalRecord;
using PawTrack.Application.Clinics.Commands.ManageApiKey;
using PawTrack.Application.Clinics.Commands.PerformClinicScan;
using PawTrack.Application.Clinics.Commands.RegisterClinic;
using PawTrack.Application.Clinics.Commands.ReviewClinic;
using PawTrack.Application.Clinics.Queries.GetClinicScanStats;
using PawTrack.Application.Clinics.Queries.GetMyClinic;
using PawTrack.Application.Clinics.Queries.GetNearbyActiveAlerts;
using PawTrack.Application.Clinics.Queries.GetPendingClinics;
using PawTrack.Application.Clinics.Queries.GetPetMedicalHistoryForClinic;
using PawTrack.Application.Clinics.Queries.GetPublicClinics;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Medical.ClinicAccess;
using PawTrack.Domain.Auth;
using PawTrack.Domain.Clinics;
using PawTrack.Domain.Medical;
using System.Security.Claims;

namespace PawTrack.API.Controllers;

[ApiController]
[Route("api/clinics")]
public sealed class ClinicsController(ISender sender, IBlobStorageService blobStorage) : ControllerBase
{
    // ── Register ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Registers a new clinic. Creates a user account (Role = Clinic, Status = Pending).
    /// Admin must manually activate the clinic before it can scan.
    /// </summary>
    [HttpPost("register")]
    [AllowAnonymous]
    [EnableRateLimiting("register")]
    [RequestSizeLimit(4096)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Register(
        [FromBody] RegisterClinicRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new RegisterClinicCommand(
                request.Name,
                request.LicenseNumber,
                request.Address,
                request.Lat,
                request.Lng,
                request.ContactEmail,
                request.Password),
            cancellationToken);

        if (result.IsFailure)
        {
            // Anti-enumeration: when the email is already in use, return 201 with a
            // generic confirmation — identical to a successful registration — so the
            // caller cannot determine whether the address is already registered.
            // Other failures (duplicate license, validation errors) are surfaced normally.
            if (result.Errors.Contains(RegisterClinicCommand.DuplicateEmailError))
                return Created(string.Empty, new
                {
                    message = "Your application has been received. " +
                              "If your clinic is eligible, you will receive a confirmation."
                });

            return UnprocessableEntity(new ProblemDetails
            {
                Title = "Clinic registration failed",
                Detail = string.Join("; ", result.Errors),
                Status = 422,
            });
        }

        return Created(string.Empty, result.Value);
    }

    // ── Get my clinic profile ─────────────────────────────────────────────────

    [HttpGet("me")]
    [Authorize(Roles = "Clinic")]
    [EnableRateLimiting("public-api")] // 30/min — each call issues GetMyClinicQuery (DB SELECT)
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMyClinic(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        var result = await sender.Send(new GetMyClinicQuery(userId), cancellationToken);

        if (result.IsFailure)
            return BadRequest(new ProblemDetails { Title = "Error", Status = 400 });

        if (result.Value is null)
            return NotFound(new ProblemDetails { Title = "Clinic profile not found", Status = 404 });

        return Ok(result.Value);
    }

    // ── Scan ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Scans a pet QR code URL or RFID chip identifier.
    /// Returns the pet owner contact if a match is found and records the audit entry.
    /// Requires an active Clinic account.
    /// </summary>
    [HttpPost("scan")]
    [Authorize(Roles = "Clinic")]
    [EnableRateLimiting("clinic-scan")] // 30/min — each scan writes DB + dispatches owner notification
    [RequestSizeLimit(2048)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Scan(
        [FromBody] ClinicScanRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        if (!Enum.TryParse<ScanInputType>(request.InputType, ignoreCase: true, out var inputType))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid input type",
                Detail = "InputType must be 'Qr' or 'RfidChip'.",
                Status = 400,
            });
        }

        // Resolve the clinic that belongs to this authenticated user
        var clinicResult = await sender.Send(new GetMyClinicQuery(userId), cancellationToken);
        if (clinicResult.IsFailure || clinicResult.Value is null)
            return Forbid();

        var result = await sender.Send(
            new PerformClinicScanCommand(clinicResult.Value.Id, request.Input, inputType),
            cancellationToken);

        return result.IsFailure
            ? BadRequest(new ProblemDetails { Title = "Scan failed", Detail = string.Join("; ", result.Errors), Status = 400 })
            : Ok(result.Value);
    }

    // ── Public directory ──────────────────────────────────────────────────────

    [HttpGet("public")]
    [AllowAnonymous]
    [EnableRateLimiting("public-api")]
    [ResponseCache(Duration = 60, VaryByQueryKeys = ["lat", "lng"])]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPublicClinics(
        [FromQuery] double? lat,
        [FromQuery] double? lng,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetPublicClinicsQuery(lat, lng), cancellationToken);
        return Ok(result.Value);
    }

    // ── Scan stats (Plus/Partner) ─────────────────────────────────────────────

    [HttpGet("me/stats")]
    [Authorize(Roles = "Clinic")]
    [EnableRateLimiting("public-api")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> GetScanStats(
        [FromQuery] int? year,
        [FromQuery] int? month,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var now = DateTimeOffset.UtcNow;
        var y = year ?? now.Year;
        var m = month ?? now.Month;

        var clinicResult = await sender.Send(new GetMyClinicQuery(userId), cancellationToken);
        if (clinicResult.IsFailure || clinicResult.Value is null) return Forbid();

        var result = await sender.Send(
            new GetClinicScanStatsQuery(clinicResult.Value.Id, userId, y, m),
            cancellationToken);

        if (result.IsFailure)
            return UnprocessableEntity(new ProblemDetails { Detail = string.Join("; ", result.Errors), Status = 422 });

        return Ok(result.Value);
    }

    // ── Nearby active alerts (Partner) ───────────────────────────────────────

    [HttpGet("me/nearby-alerts")]
    [Authorize(Roles = "Clinic")]
    [EnableRateLimiting("public-api")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> GetNearbyAlerts(
        [FromQuery] double radiusKm = 15,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var clinicResult = await sender.Send(new GetMyClinicQuery(userId), cancellationToken);
        if (clinicResult.IsFailure || clinicResult.Value is null) return Forbid();

        var result = await sender.Send(
            new GetNearbyActiveAlertsQuery(clinicResult.Value.Id, userId, radiusKm),
            cancellationToken);

        if (result.IsFailure)
            return UnprocessableEntity(new ProblemDetails { Detail = string.Join("; ", result.Errors), Status = 422 });

        return Ok(result.Value);
    }

    // ── Logo upload ───────────────────────────────────────────────────────────

    [HttpPost("me/logo")]
    [Authorize(Roles = "Clinic")]
    [EnableRateLimiting("public-api")]
    [RequestSizeLimit(3 * 1024 * 1024)] // 3MB
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UploadLogo(
        IFormFile file,
        [FromServices] IClinicRepository clinicRepository,
        [FromServices] IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var allowed = new[] { "image/jpeg", "image/png", "image/webp" };
        if (!allowed.Contains(file.ContentType, StringComparer.OrdinalIgnoreCase))
            return BadRequest(new ProblemDetails { Detail = "Only JPEG, PNG or WebP images are accepted.", Status = 400 });

        // Single query — resolve user → clinic in one trip
        var clinicResult = await sender.Send(new GetMyClinicQuery(userId), cancellationToken);
        if (clinicResult.IsFailure || clinicResult.Value is null) return Forbid();

        // Fetch as tracked so EF picks up mutations
        var clinic = await clinicRepository.GetByIdAsync(clinicResult.Value.Id, cancellationToken);
        if (clinic is null) return Forbid();

        var ext = file.ContentType.Contains("png") ? "png" : file.ContentType.Contains("webp") ? "webp" : "jpg";
        var blobName = $"clinic-logos/{clinic.Id}.{ext}";

        using var stream = file.OpenReadStream();
        var url = await blobStorage.UploadAsync("clinic-logos", blobName, stream, file.ContentType, cancellationToken);

        clinic.SetLogoUrl(url);
        clinicRepository.Update(clinic);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Ok(new { logoUrl = url });
    }

    // ── API Keys (Partner) ────────────────────────────────────────────────────

    [HttpGet("me/api-keys")]
    [Authorize(Roles = "Clinic")]
    [EnableRateLimiting("public-api")]
    public async Task<IActionResult> GetApiKeys(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var clinicResult = await sender.Send(new GetMyClinicQuery(userId), cancellationToken);
        if (clinicResult.IsFailure || clinicResult.Value is null) return Forbid();

        var result = await sender.Send(
            new GetClinicApiKeysQuery(clinicResult.Value.Id, userId), cancellationToken);

        return result.IsSuccess ? Ok(result.Value)
            : UnprocessableEntity(new ProblemDetails { Detail = string.Join("; ", result.Errors) });
    }

    [HttpPost("me/api-keys")]
    [Authorize(Roles = "Clinic")]
    [EnableRateLimiting("public-api")]
    [RequestSizeLimit(512)]
    public async Task<IActionResult> CreateApiKey(
        [FromBody] CreateApiKeyRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var clinicResult = await sender.Send(new GetMyClinicQuery(userId), cancellationToken);
        if (clinicResult.IsFailure || clinicResult.Value is null) return Forbid();

        var result = await sender.Send(
            new CreateClinicApiKeyCommand(clinicResult.Value.Id, userId, request.Label),
            cancellationToken);

        if (result.IsFailure)
            return UnprocessableEntity(new ProblemDetails { Detail = string.Join("; ", result.Errors), Status = 422 });

        return Created(string.Empty, result.Value);
    }

    [HttpDelete("me/api-keys/{keyId:guid}")]
    [Authorize(Roles = "Clinic")]
    [EnableRateLimiting("public-api")]
    public async Task<IActionResult> RevokeApiKey(Guid keyId, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var clinicResult = await sender.Send(new GetMyClinicQuery(userId), cancellationToken);
        if (clinicResult.IsFailure || clinicResult.Value is null) return Forbid();

        var result = await sender.Send(
            new RevokeClinicApiKeyCommand(keyId, clinicResult.Value.Id, userId),
            cancellationToken);

        return result.IsSuccess ? NoContent()
            : NotFound(new ProblemDetails { Detail = "Key not found.", Status = 404 });
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private bool TryGetUserId(out Guid userId)
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(raw, out userId);
    }

    private bool TryGetRole(out UserRole role)
    {
        var claim = User.FindFirstValue(ClaimTypes.Role);
        return Enum.TryParse(claim, true, out role);
    }

    // ── Admin endpoints ───────────────────────────────────────────────────────

    [HttpGet("admin/pending")]
    [Authorize(Roles = "Admin")]
    [EnableRateLimiting("public-api")] // 30/min — Admin-only but unthrottled DB SELECT still opens DoS vector
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetPendingClinics(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetPendingClinicsQuery(), cancellationToken);
        return Ok(result.Value);
    }

    [HttpPut("admin/{clinicId:guid}/review")]
    [Authorize(Roles = "Admin")]
    [EnableRateLimiting("public-api")]
    [RequestSizeLimit(512)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ReviewClinic(
        Guid clinicId,
        [FromBody] ReviewClinicRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ReviewClinicCommand(clinicId, request.Approve), cancellationToken);
        if (result.IsFailure)
            return NotFound(new ProblemDetails { Title = "Clinic not found", Status = 404 });

        return NoContent();
    }

    // ── Expediente digital ─────────────────────────────────────────────────────

    /// <summary>
    /// Returns a pet's full medical history to an authenticated clinic.
    /// Access: Option A — clinic has a ClinicScan for this pet in the last 90 days.
    ///         Option B — caller supplies petId from the current consult's QR/chip result.
    /// Supply either petId (A) or qrOrChipInput+inputType (B).
    /// </summary>
    [HttpGet("patients/{petId:guid}/medical")]
    [Authorize(Roles = "Clinic")]
    [EnableRateLimiting("public-api")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetPatientMedicalHistory(
        Guid petId,
        CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var clinicResult = await sender.Send(new GetMyClinicQuery(userId), ct);
        if (clinicResult.IsFailure || clinicResult.Value is null) return Forbid();

        var result = await sender.Send(
            new GetPetMedicalHistoryForClinicQuery(clinicResult.Value.Id, petId, null, null), ct);

        if (result.IsFailure)
            return StatusCode(403, new ProblemDetails { Detail = result.Errors.FirstOrDefault(), Status = 403 });

        return Ok(result.Value);
    }

    /// <summary>
    /// Adds a medical record to a pet's expediente from an authenticated clinic.
    /// Option A: petId is known from a previous scan (clinic has scan history for this pet).
    /// Option B: qrOrChipInput provided — scan is created inline (records this consult visit).
    /// </summary>
    [HttpPost("patients/medical")]
    [Authorize(Roles = "Clinic")]
    [Consumes("multipart/form-data")]
    [EnableRateLimiting("public-api")]
    [RequestSizeLimit(5_242_880)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> AddPatientMedicalRecord(
        [FromForm] ClinicAddMedicalRecordRequest request,
        CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var clinicResult = await sender.Send(new GetMyClinicQuery(userId), ct);
        if (clinicResult.IsFailure || clinicResult.Value is null) return Forbid();

        if (!Enum.TryParse<MedicalRecordType>(request.RecordType, ignoreCase: true, out var recordType))
            return BadRequest(new ProblemDetails { Detail = $"Tipo inválido: {request.RecordType}.", Status = 400 });

        ScanInputType? inputType = null;
        if (!string.IsNullOrWhiteSpace(request.InputType)
            && Enum.TryParse<ScanInputType>(request.InputType, ignoreCase: true, out var parsedInputType))
            inputType = parsedInputType;

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

        var result = await sender.Send(new AddClinicMedicalRecordCommand(
            clinicResult.Value.Id, userId,
            request.PetId, request.QrOrChipInput, inputType,
            recordType, request.Date, request.Description,
            request.VetName, request.NextDueDate,
            docBytes, docContentType), ct);

        if (result.IsFailure)
            return result.Errors.Any(e => e.Contains("acceso") || e.Contains("escaneo"))
                ? StatusCode(403, new ProblemDetails { Detail = result.Errors.FirstOrDefault(), Status = 403 })
                : UnprocessableEntity(new ProblemDetails { Detail = string.Join("; ", result.Errors), Status = 422 });

        return Created(string.Empty, result.Value);
    }

    // ── Access grants (Option C) ───────────────────────────────────────────────

    /// <summary>
    /// Clinic generates an 8-char code to hand to the pet owner.
    /// Requires prior scan history with this pet (Option A gate).
    /// Owner enters the code to activate permanent access.
    /// </summary>
    [HttpPost("access-grants/code")]
    [Authorize(Roles = "Clinic")]
    [EnableRateLimiting("public-api")]
    [RequestSizeLimit(256)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> GenerateAccessCode(
        [FromBody] ClinicGenerateAccessCodeRequest request,
        CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var clinicResult = await sender.Send(new GetMyClinicQuery(userId), ct);
        if (clinicResult.IsFailure || clinicResult.Value is null) return Forbid();

        var result = await sender.Send(
            new ClinicGenerateAccessCodeCommand(clinicResult.Value.Id, userId, request.PetId), ct);

        if (result.IsFailure)
            return UnprocessableEntity(new ProblemDetails { Detail = result.Errors.FirstOrDefault(), Status = 422 });

        return Created(string.Empty, result.Value);
    }

    /// <summary>Clinic enters the code the owner generated to activate a grant.</summary>
    [HttpPost("access-grants/accept")]
    [Authorize(Roles = "Clinic")]
    [EnableRateLimiting("public-api")]
    [RequestSizeLimit(256)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> AcceptOwnerCode(
        [FromBody] AcceptGrantCodeRequest request,
        CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var clinicResult = await sender.Send(new GetMyClinicQuery(userId), ct);
        if (clinicResult.IsFailure || clinicResult.Value is null) return Forbid();

        var result = await sender.Send(
            new ClinicAcceptOwnerCodeCommand(clinicResult.Value.Id, userId, request.Code), ct);

        if (result.IsFailure)
            return UnprocessableEntity(new ProblemDetails { Detail = result.Errors.FirstOrDefault(), Status = 422 });

        return Ok(result.Value);
    }

    /// <summary>List all pets the clinic has active permanent grants for.</summary>
    [HttpGet("access-grants")]
    [Authorize(Roles = "Clinic")]
    [EnableRateLimiting("public-api")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAuthorizedPets(CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var clinicResult = await sender.Send(new GetMyClinicQuery(userId), ct);
        if (clinicResult.IsFailure || clinicResult.Value is null) return Forbid();

        var result = await sender.Send(
            new GetClinicAuthorizedPetsQuery(clinicResult.Value.Id), ct);

        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Errors);
    }
}

// ── Request models ────────────────────────────────────────────────────────────

public sealed record RegisterClinicRequest(
    string Name,
    string LicenseNumber,
    string Address,
    decimal Lat,
    decimal Lng,
    string ContactEmail,
    string Password);

public sealed record ClinicScanRequest(
    string Input,
    string InputType);

public sealed record ReviewClinicRequest(bool Approve);

public sealed record CreateApiKeyRequest(string Label);

/// <summary>
/// Multipart form for POST /api/clinics/patients/medical.
/// Supply PetId (Option A — prior scan required) or QrOrChipInput+InputType (Option B — inline scan).
/// </summary>
public sealed class ClinicAddMedicalRecordRequest
{
    public Guid? PetId { get; init; }
    public string? QrOrChipInput { get; init; }
    public string? InputType { get; init; }
    public string RecordType { get; init; } = string.Empty;
    public DateOnly Date { get; init; }
    public string Description { get; init; } = string.Empty;
    public string? VetName { get; init; }
    public DateOnly? NextDueDate { get; init; }
    public IFormFile? Document { get; init; }
}

public sealed record ClinicGenerateAccessCodeRequest(Guid PetId);
public sealed record AcceptGrantCodeRequest(string Code);
