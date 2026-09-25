using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PawTrack.Application.Clinics.Commands.AddClinicMedicalRecord;
using PawTrack.Application.Clinics.Commands.ManageApiKey;
using PawTrack.Application.Clinics.Commands.PerformClinicScan;
using PawTrack.Application.Clinics.Commands.RegisterClinic;
using PawTrack.Application.Clinics.Commands.ReviewClinic;
using PawTrack.Application.Clinics.Commands.TrackClinicView;
using PawTrack.Application.Clinics.Commands.UpdateClinicProfile;
using PawTrack.Application.Clinics.Commands.SubmitClinicProfileChange;
using PawTrack.Application.Clinics.Commands.ReviewClinicProfileChange;
using PawTrack.Application.Clinics.Queries.GetClinicScanStats;
using PawTrack.Application.Clinics.Queries.GetMyClinic;
using PawTrack.Application.Clinics.Queries.GetNearbyActiveAlerts;
using PawTrack.Application.Clinics.Queries.GetPendingClinics;
using PawTrack.Application.Clinics.Queries.GetPetMedicalHistoryForClinic;
using PawTrack.Application.Clinics.Queries.GetPublicClinics;
using PawTrack.Application.Clinics.Queries.SearchClinicsForAccess;
using PawTrack.Application.Certificates.Commands.ManageCertificateIssuers;
using PawTrack.Application.Certificates.Queries.GetClinicCertificateIssuers;
using PawTrack.Application.Certificates.Commands.ScheduleVeterinarianAppointment;
using PawTrack.Application.Certificates.Commands.RescheduleVeterinarianAppointment;
using PawTrack.Application.Certificates.Commands.SetVeterinarianPermissions;
using PawTrack.Application.Certificates.Commands.UpdateVeterinarianAppointmentStatus;
using PawTrack.Application.Certificates.Commands.CreateVeterinarianScheduleBlock;
using PawTrack.Application.Certificates.Commands.DeleteVeterinarianScheduleBlock;
using PawTrack.Application.Certificates.Commands.UpdateVeterinarianScheduleBlock;
using PawTrack.Application.Certificates.Queries.GetClinicAgenda;
using PawTrack.Application.Certificates.Queries.GetClinicScheduleBlocks;
using PawTrack.Application.Audit;
using PawTrack.Application.Clinics.Commands.CloseClinicalConsultation;
using PawTrack.Application.Clinics.Commands.CreateClinicalConsultation;
using PawTrack.Application.Clinics.Commands.UploadClinicalConsultationAttachment;
using PawTrack.Application.Clinics.Commands.ManageClinicInventory;
using PawTrack.Application.Clinics.Commands.ManageClinicBilling;
using PawTrack.Application.Clinics.Commands.ManageClinicFinanceAccess;
using PawTrack.Application.Clinics.Commands.ManageClinicStaff;
using PawTrack.Application.Clinics.Commands.ManageClinicCrm;
using PawTrack.Application.Clinics.Queries.DownloadClinicalConsultationPrescription;
using PawTrack.Application.Clinics.Queries.GetClinicalConsultationTemplates;
using PawTrack.Application.Clinics.Commands.ExportClinicMedical;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Medical.ClinicAccess;
using PawTrack.Application.Pets.SanitaryIdentity;
using PawTrack.Domain.Auth;
using PawTrack.Domain.Audit;
using PawTrack.Domain.Clinics;
using PawTrack.Domain.Medical;
using System.Security.Claims;

namespace PawTrack.API.Controllers;

[ApiController]
[Route("api/clinics")]
[Route("api/v1/clinics")]
[ApiVersion("1.0")]
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

    [HttpPut("me/profile")]
    [Authorize(Roles = "Clinic")]
    [EnableRateLimiting("public-api")]
    [RequestSizeLimit(2048)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateMyProfile(
        [FromBody] UpdateClinicProfileRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var result = await sender.Send(new UpdateClinicProfileCommand(
            userId,
            request.Name,
            request.Address,
            request.PhoneNumber,
            request.Website,
            request.IsEmergency24h,
            request.EmergencyPhone,
            request.Description,
            request.Services,
            request.OpeningHours,
            request.WhatsAppNumber,
            request.IsWhatsAppContactEnabled), cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : StatusCode(StatusCodes.Status403Forbidden,
                new ProblemDetails { Detail = string.Join("; ", result.Errors), Status = 403 });
    }

    [HttpPost("me/profile-changes")]
    [Authorize(Roles = "Clinic")]
    [EnableRateLimiting("public-api")]
    public async Task<IActionResult> SubmitProfileChange(
        [FromBody] UpdateClinicProfileRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var clinicResult = await sender.Send(new GetMyClinicQuery(userId), cancellationToken);
        if (clinicResult.IsFailure || clinicResult.Value is null) return Forbid();
        var result = await sender.Send(new SubmitClinicProfileChangeCommand(
            clinicResult.Value.Id, userId, request.Name, request.Address,
            request.PhoneNumber, request.Website, request.IsEmergency24h,
            request.EmergencyPhone, request.Description, request.Services,
            request.OpeningHours), cancellationToken);
        return result.IsSuccess ? Accepted(result.Value) : UnprocessableEntity(result.Errors);
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
            new PerformClinicScanCommand(clinicResult.Value.Id, request.Input, inputType,
                Request.Headers["Idempotency-Key"].FirstOrDefault()),
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
        [FromQuery] string? search,
        [FromQuery] bool emergencyOnly = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 24,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetPublicClinicsQuery(
            lat, lng, Search: search, EmergencyOnly: emergencyOnly, Page: page, PageSize: pageSize), cancellationToken);
        return Ok(result.Value);
    }

    [HttpGet("public/{clinicId:guid}")]
    [AllowAnonymous]
    [EnableRateLimiting("public-api")]
    [ResponseCache(Duration = 60)]
    public async Task<IActionResult> GetPublicClinicProfile(
        Guid clinicId,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new GetPublicClinicProfileQuery(clinicId), cancellationToken);
        return result.Value is null ? NotFound() : Ok(result.Value);
    }

    /// <summary>Search active clinics by name or license number, for authorizing medical access to a pet's record.</summary>
    [HttpGet("search")]
    [Authorize]
    [EnableRateLimiting("public-api")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> SearchForAccess([FromQuery] string query, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new SearchClinicsForAccessQuery(query), cancellationToken);
        return result.IsSuccess
            ? Ok(result.Value)
            : UnprocessableEntity(new ProblemDetails { Detail = string.Join("; ", result.Errors), Status = 422 });
    }

    // ── View tracking — fire-and-forget, anonymous ────────────────────────────
    /// <summary>
    /// Records a clinic profile impression. Called by the frontend when a user opens a
    /// clinic popup in the map ("map") or visits the clinic directory ("directory").
    /// Best-effort: always returns 204 regardless of storage outcome.
    /// </summary>
    [HttpPost("{clinicId:guid}/view")]
    [AllowAnonymous]
    [EnableRateLimiting("public-api")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public IActionResult TrackView(Guid clinicId, [FromQuery] string source = "map")
    {
        var allowed = new[] { "map", "directory", "search", "alert" };
        var safeSource = allowed.Contains(source, StringComparer.OrdinalIgnoreCase)
            ? source.ToLowerInvariant()
            : "map";

        // IP hash for deduplication — never stored raw (OWASP PII requirement)
        var rawIp = HttpContext.Connection.RemoteIpAddress?.ToString();
        string? ipHash = rawIp is not null
            ? Convert.ToHexString(
                System.Security.Cryptography.SHA256.HashData(
                    System.Text.Encoding.UTF8.GetBytes(rawIp)))
                .ToLowerInvariant()
            : null;

        _ = sender.Send(new TrackClinicViewCommand(clinicId, safeSource, ipHash));
        return NoContent();
    }

    // ── Visibility stats (Plus/Partner) ──────────────────────────────────────

    [HttpGet("me/visibility-stats")]
    [Authorize(Roles = "Clinic")]
    [EnableRateLimiting("public-api")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status402PaymentRequired)]
    public async Task<IActionResult> GetVisibilityStats(
        [FromQuery] int days = 30,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var clinicResult = await sender.Send(new GetMyClinicQuery(userId), cancellationToken);
        if (clinicResult.IsFailure || clinicResult.Value is null) return Forbid();

        var result = await sender.Send(
            new GetClinicVisibilityStatsQuery(clinicResult.Value.Id, days), cancellationToken);

        if (result.IsFailure)
            return StatusCode(402, new ProblemDetails { Detail = result.Errors.FirstOrDefault(), Status = 402 });

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
            new CreateClinicApiKeyCommand(clinicResult.Value.Id, userId, request.Label, request.Scopes),
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

    // ── POST /api/clinics/me/api-keys/{id}/rotate — revoke old, issue new ─────
    [HttpPost("me/api-keys/{keyId:guid}/rotate")]
    [Authorize(Roles = "Clinic")]
    [EnableRateLimiting("public-api")]
    public async Task<IActionResult> RotateApiKey(Guid keyId, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var clinicResult = await sender.Send(new GetMyClinicQuery(userId), cancellationToken);
        if (clinicResult.IsFailure || clinicResult.Value is null) return Forbid();

        var result = await sender.Send(
            new RotateClinicApiKeyCommand(keyId, clinicResult.Value.Id, userId),
            cancellationToken);

        if (result.IsFailure)
            return UnprocessableEntity(new ProblemDetails { Detail = string.Join("; ", result.Errors), Status = 422 });

        return Ok(result.Value);
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

    private static string EscapeCsv(string? value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        return $"\"{value.Replace("\"", "\"\"")}\"";
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

    [HttpPut("admin/profile-changes/{changeId:guid}/review")]
    [Authorize(Roles = "Admin")]
    [EnableRateLimiting("public-api")]
    public async Task<IActionResult> ReviewProfileChange(
        Guid changeId,
        [FromBody] ReviewProfileChangeRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var adminUserId)) return Unauthorized();
        var result = await sender.Send(new ReviewClinicProfileChangeCommand(
            changeId, adminUserId, request.Approve, request.Reason), cancellationToken);
        return result.IsSuccess ? Ok() : UnprocessableEntity(result.Errors);
    }

    [HttpGet("admin/profile-changes/pending")]
    [Authorize(Roles = "Admin")]
    [EnableRateLimiting("public-api")]
    public async Task<IActionResult> GetPendingProfileChanges(CancellationToken cancellationToken)
    {
        var repository = HttpContext.RequestServices.GetRequiredService<IClinicProfileChangeRepository>();
        var changes = await repository.GetPendingAsync(100, cancellationToken);
        return Ok(changes.Select(SubmitClinicProfileChangeCommandHandler.ToDto));
    }

    [HttpPut("admin/{clinicId:guid}/certificate-verification")]
    [Authorize(Roles = "Admin")]
    [EnableRateLimiting("public-api")]
    [RequestSizeLimit(512)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> VerifyClinicForCertificates(
        Guid clinicId,
        [FromBody] VerifyClinicForCertificatesRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var adminUserId)) return Unauthorized();

        var result = await sender.Send(
            new VerifyClinicForCertificatesCommand(clinicId, adminUserId, request.ExpiresAt),
            cancellationToken);

        if (result.IsFailure)
            return UnprocessableEntity(new ProblemDetails { Detail = string.Join("; ", result.Errors), Status = 422 });

        return Ok(result.Value);
    }

    [HttpGet("admin/verifications")]
    [Authorize(Roles = "Admin")]
    [EnableRateLimiting("public-api")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetClinicVerificationsForAdmin(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetClinicVerificationsForAdminQuery(page, pageSize), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Errors);
    }

    [HttpPut("admin/verifications/{verificationId:guid}/review")]
    [Authorize(Roles = "Admin")]
    [EnableRateLimiting("public-api")]
    [RequestSizeLimit(1024)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ReviewClinicVerification(
        Guid verificationId,
        [FromBody] ReviewVerificationRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var adminUserId)) return Unauthorized();

        var result = await sender.Send(new ReviewClinicVerificationCommand(
            verificationId, adminUserId, request.Approve, request.ExpiresAt, request.Reason, request.Notes), cancellationToken);

        return result.IsSuccess ? Ok(result.Value)
            : UnprocessableEntity(new ProblemDetails { Detail = string.Join("; ", result.Errors), Status = 422 });
    }

    [HttpGet("admin/verifications/{verificationId:guid}/document")]
    [Authorize(Roles = "Admin")]
    [EnableRateLimiting("public-api")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> DownloadClinicVerificationDocument(Guid verificationId, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var adminUserId)) return Unauthorized();
        var result = await sender.Send(new DownloadClinicVerificationDocumentQuery(verificationId, adminUserId, IsAdmin: true), cancellationToken);
        return result.IsSuccess
            ? File(result.Value!.Bytes, result.Value.ContentType, result.Value.FileName)
            : UnprocessableEntity(new ProblemDetails { Detail = string.Join("; ", result.Errors), Status = 422 });
    }

    [HttpGet("admin/veterinarians")]
    [Authorize(Roles = "Admin")]
    [EnableRateLimiting("public-api")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetVeterinariansForAdmin(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetClinicVeterinariansForAdminQuery(page, pageSize), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Errors);
    }

    [HttpPut("admin/veterinarians/{veterinarianId:guid}/review")]
    [Authorize(Roles = "Admin")]
    [EnableRateLimiting("public-api")]
    [RequestSizeLimit(1024)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ReviewVeterinarian(
        Guid veterinarianId,
        [FromBody] ReviewVerificationRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var adminUserId)) return Unauthorized();
        var result = await sender.Send(new ReviewClinicVeterinarianCommand(
            veterinarianId, adminUserId, request.Approve, request.ExpiresAt, request.Reason, request.Notes), cancellationToken);

        return result.IsSuccess ? Ok(result.Value)
            : UnprocessableEntity(new ProblemDetails { Detail = string.Join("; ", result.Errors), Status = 422 });
    }

    [HttpPost("admin/veterinarians/{veterinarianId:guid}/suspend")]
    [Authorize(Roles = "Admin")]
    [EnableRateLimiting("public-api")]
    [RequestSizeLimit(512)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> SuspendVeterinarian(
        Guid veterinarianId,
        [FromBody] ReasonRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var adminUserId)) return Unauthorized();
        var result = await sender.Send(new SuspendClinicVeterinarianCommand(veterinarianId, adminUserId, request.Reason), cancellationToken);
        return result.IsSuccess ? Ok(result.Value)
            : UnprocessableEntity(new ProblemDetails { Detail = string.Join("; ", result.Errors), Status = 422 });
    }

    [HttpGet("admin/veterinarians/{veterinarianId:guid}/document")]
    [Authorize(Roles = "Admin")]
    [EnableRateLimiting("public-api")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> DownloadVeterinarianDocumentForAdmin(Guid veterinarianId, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var adminUserId)) return Unauthorized();
        var result = await sender.Send(new DownloadVeterinarianDocumentQuery(veterinarianId, adminUserId, IsAdmin: true), cancellationToken);
        return result.IsSuccess
            ? File(result.Value!.Bytes, result.Value.ContentType, result.Value.FileName)
            : UnprocessableEntity(new ProblemDetails { Detail = string.Join("; ", result.Errors), Status = 422 });
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
            new GetPetMedicalHistoryForClinicQuery(clinicResult.Value.Id, petId, null, null, userId), ct);

        if (result.IsFailure)
            return StatusCode(403, new ProblemDetails { Detail = result.Errors.FirstOrDefault(), Status = 403 });

        return Ok(result.Value);
    }

    [HttpGet("patients/{petId:guid}/sanitary-identity")]
    [Authorize(Roles = "Clinic")]
    [EnableRateLimiting("public-api")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPatientSanitaryIdentity(Guid petId, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var clinicResult = await sender.Send(new GetMyClinicQuery(userId), ct);
        if (clinicResult.IsFailure || clinicResult.Value is null) return Forbid();

        var result = await sender.Send(new GetClinicPetSanitaryIdentityQuery(petId, clinicResult.Value.Id, userId), ct);
        return result.IsSuccess ? Ok(result.Value)
            : StatusCode(403, new ProblemDetails { Detail = result.Errors.FirstOrDefault(), Status = 403 });
    }

    [HttpPost("patients/{petId:guid}/microchip/verify")]
    [Authorize(Roles = "Clinic")]
    [EnableRateLimiting("clinic-scan")]
    [RequestSizeLimit(512)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> VerifyPatientMicrochip(
        Guid petId,
        [FromBody] VerifyMicrochipRequest request,
        CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var clinicResult = await sender.Send(new GetMyClinicQuery(userId), ct);
        if (clinicResult.IsFailure || clinicResult.Value is null) return Forbid();

        var result = await sender.Send(new VerifyPetMicrochipCommand(
            petId, clinicResult.Value.Id, userId, request.ObservedChipId, request.Notes), ct);

        return result.IsSuccess ? Ok(result.Value)
            : UnprocessableEntity(new ProblemDetails { Detail = string.Join("; ", result.Errors), Status = 422 });
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

    [HttpGet("me/certificate-issuers")]
    [Authorize(Roles = "Clinic")]
    [EnableRateLimiting("public-api")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCertificateIssuers(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var clinicResult = await sender.Send(new GetMyClinicQuery(userId), cancellationToken);
        if (clinicResult.IsFailure || clinicResult.Value is null) return Forbid();

        var result = await sender.Send(
            new GetClinicCertificateIssuersQuery(clinicResult.Value.Id, userId),
            cancellationToken);

        return result.IsSuccess ? Ok(result.Value)
            : UnprocessableEntity(new ProblemDetails { Detail = string.Join("; ", result.Errors), Status = 422 });
    }

    [HttpPost("me/veterinarians/{veterinarianId:guid}/appointments")]
    [Authorize(Roles = "Clinic")]
    [EnableRateLimiting("public-api")]
    public async Task<IActionResult> ScheduleVeterinarianAppointment(
        Guid veterinarianId,
        [FromBody] ScheduleVeterinarianAppointmentRequest request,
        CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var clinicResult = await sender.Send(new GetMyClinicQuery(userId), ct);
        if (clinicResult.IsFailure || clinicResult.Value is null) return Forbid();
        var result = await sender.Send(new ScheduleVeterinarianAppointmentCommand(
            clinicResult.Value.Id, userId, veterinarianId, request.PetId,
            request.StartsAt, request.DurationMinutes), ct);
        return result.IsSuccess ? Created(string.Empty, new { appointmentId = result.Value }) : UnprocessableEntity(result.Errors);
    }

    [HttpPost("me/veterinarians/{veterinarianId:guid}/blocks")]
    [Authorize(Roles = "Clinic")]
    [EnableRateLimiting("public-api")]
    public async Task<IActionResult> CreateVeterinarianScheduleBlock(
        Guid veterinarianId,
        [FromBody] CreateVeterinarianScheduleBlockRequest request,
        CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var clinicResult = await sender.Send(new GetMyClinicQuery(userId), ct);
        if (clinicResult.IsFailure || clinicResult.Value is null) return Forbid();

        var result = await sender.Send(new CreateVeterinarianScheduleBlockCommand(
            clinicResult.Value.Id,
            userId,
            veterinarianId,
            request.StartsAt,
            request.EndsAt,
            request.Reason), ct);

        return result.IsSuccess ? Created(string.Empty, new { blockId = result.Value }) : UnprocessableEntity(result.Errors);
    }

    [HttpGet("me/schedule-blocks")]
    [Authorize(Roles = "Clinic")]
    [EnableRateLimiting("public-api")]
    public async Task<IActionResult> GetVeterinarianScheduleBlocks(
        [FromQuery] DateTimeOffset from,
        [FromQuery] DateTimeOffset to,
        CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var clinicResult = await sender.Send(new GetMyClinicQuery(userId), ct);
        if (clinicResult.IsFailure || clinicResult.Value is null) return Forbid();

        var result = await sender.Send(new GetClinicScheduleBlocksQuery(
            clinicResult.Value.Id,
            userId,
            from,
            to), ct);

        return result.IsSuccess ? Ok(result.Value) : UnprocessableEntity(result.Errors);
    }

    [HttpPatch("me/schedule-blocks/{blockId:guid}")]
    [Authorize(Roles = "Clinic")]
    [EnableRateLimiting("public-api")]
    public async Task<IActionResult> UpdateVeterinarianScheduleBlock(
        Guid blockId,
        [FromBody] UpdateVeterinarianScheduleBlockRequest request,
        CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var clinicResult = await sender.Send(new GetMyClinicQuery(userId), ct);
        if (clinicResult.IsFailure || clinicResult.Value is null) return Forbid();

        var result = await sender.Send(new UpdateVeterinarianScheduleBlockCommand(
            clinicResult.Value.Id,
            userId,
            blockId,
            request.StartsAt,
            request.EndsAt,
            request.Reason), ct);

        return result.IsSuccess ? NoContent() : UnprocessableEntity(result.Errors);
    }

    [HttpDelete("me/schedule-blocks/{blockId:guid}")]
    [Authorize(Roles = "Clinic")]
    [EnableRateLimiting("public-api")]
    public async Task<IActionResult> DeleteVeterinarianScheduleBlock(
        Guid blockId,
        CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var clinicResult = await sender.Send(new GetMyClinicQuery(userId), ct);
        if (clinicResult.IsFailure || clinicResult.Value is null) return Forbid();

        var result = await sender.Send(new DeleteVeterinarianScheduleBlockCommand(
            clinicResult.Value.Id,
            userId,
            blockId), ct);

        return result.IsSuccess ? NoContent() : UnprocessableEntity(result.Errors);
    }

    [HttpGet("me/agenda-audit")]
    [Authorize(Roles = "Clinic")]
    [EnableRateLimiting("public-api")]
    public async Task<IActionResult> GetClinicAgendaAudit(
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        [FromQuery] string? format,
        CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var clinicResult = await sender.Send(new GetMyClinicQuery(userId), ct);
        if (clinicResult.IsFailure || clinicResult.Value is null) return Forbid();

        var result = await sender.Send(new GetAuditLogQuery(
            EntityType: null,
            EntityId: null,
            Take: 200,
            ActorId: userId,
            From: from,
            To: to), ct);
        if (result.IsFailure) return UnprocessableEntity(result.Errors);

        var actions = new HashSet<string>(StringComparer.Ordinal)
        {
            AuditAction.ClinicAppointmentScheduled.ToString(),
            AuditAction.ClinicAppointmentStatusChanged.ToString(),
            AuditAction.ClinicAppointmentRescheduled.ToString(),
            AuditAction.ClinicScheduleBlockCreated.ToString(),
            AuditAction.ClinicScheduleBlockUpdated.ToString(),
            AuditAction.ClinicScheduleBlockDeleted.ToString(),
        };
        var entries = result.Value.Where(entry => actions.Contains(entry.Action)).ToList();

        if (string.Equals(format, "csv", StringComparison.OrdinalIgnoreCase))
        {
            var csv = "performedAt,action,entityType,entityId,details\n" + string.Join("\n", entries.Select(entry =>
                $"{entry.PerformedAt:O},{entry.Action},{entry.EntityType},{entry.EntityId},{EscapeCsv(entry.Details)}"));
            return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv", "clinic-agenda-audit.csv");
        }

        return Ok(entries);
    }

    [HttpGet("me/appointments")]
    [Authorize(Roles = "Clinic")]
    [EnableRateLimiting("public-api")]
    public async Task<IActionResult> GetVeterinarianAgenda(
        [FromQuery] DateTimeOffset from,
        [FromQuery] DateTimeOffset to,
        CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var clinicResult = await sender.Send(new GetMyClinicQuery(userId), ct);
        if (clinicResult.IsFailure || clinicResult.Value is null) return Forbid();

        var result = await sender.Send(new GetClinicAgendaQuery(
            clinicResult.Value.Id,
            userId,
            from,
            to), ct);

        return result.IsSuccess ? Ok(result.Value) : UnprocessableEntity(result.Errors);
    }

    [HttpGet("staff-workspaces")]
    [Authorize]
    [EnableRateLimiting("public-api")]
    public async Task<IActionResult> GetStaffWorkspaces(CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        return Ok(await sender.Send(new GetMyClinicStaffWorkspacesQuery(userId), ct));
    }

    [HttpGet("me/staff/members")]
    [Authorize(Roles = "Clinic")]
    [EnableRateLimiting("public-api")]
    public async Task<IActionResult> GetClinicStaffMembers(CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var clinic = await sender.Send(new GetMyClinicQuery(userId), ct);
        if (clinic.IsFailure || clinic.Value is null) return Forbid();
        var result = await sender.Send(new GetClinicStaffMembersQuery(clinic.Value.Id, userId), ct);
        return result.IsSuccess ? Ok(result.Value) : Forbid();
    }

    [HttpPut("me/staff/members")]
    [Authorize(Roles = "Clinic", Policy = "ClinicFinanceMfa")]
    [EnableRateLimiting("public-api")]
    public async Task<IActionResult> GrantClinicStaffMember([FromBody] GrantClinicStaffMemberRequest request, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var clinic = await sender.Send(new GetMyClinicQuery(userId), ct);
        if (clinic.IsFailure || clinic.Value is null) return Forbid();
        if (!Enum.TryParse<ClinicStaffRole>(request.Role, true, out var role) || !Enum.IsDefined(role)) return BadRequest();
        var result = await sender.Send(new GrantClinicStaffMembershipCommand(clinic.Value.Id, userId,
            request.Email, role, request.VeterinarianId), ct);
        return result.IsSuccess ? Ok(new { membershipId = result.Value }) : UnprocessableEntity(result.Errors);
    }

    [HttpDelete("me/staff/members/{memberUserId:guid}")]
    [Authorize(Roles = "Clinic", Policy = "ClinicFinanceMfa")]
    [EnableRateLimiting("public-api")]
    public async Task<IActionResult> RevokeClinicStaffMember(Guid memberUserId, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var clinic = await sender.Send(new GetMyClinicQuery(userId), ct);
        if (clinic.IsFailure || clinic.Value is null) return Forbid();
        var result = await sender.Send(new RevokeClinicStaffMembershipCommand(clinic.Value.Id, userId, memberUserId), ct);
        return result.IsSuccess ? NoContent() : UnprocessableEntity(result.Errors);
    }

    [HttpGet("{clinicId:guid}/staff/appointments")]
    [Authorize]
    [EnableRateLimiting("public-api")]
    public async Task<IActionResult> GetStaffAgenda(Guid clinicId, [FromQuery] DateTimeOffset from, [FromQuery] DateTimeOffset to, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await sender.Send(new GetClinicAgendaQuery(clinicId, userId, from, to), ct);
        return result.IsSuccess ? Ok(result.Value) : UnprocessableEntity(result.Errors);
    }

    [HttpPatch("{clinicId:guid}/staff/appointments/{appointmentId:guid}/status")]
    [Authorize]
    [EnableRateLimiting("public-api")]
    public async Task<IActionResult> UpdateStaffAppointmentStatus(Guid clinicId, Guid appointmentId,
        [FromBody] UpdateVeterinarianAppointmentStatusRequest request, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        if (!Enum.TryParse<PawTrack.Domain.Certificates.VeterinarianAppointmentStatus>(request.Status, true, out var status) || !Enum.IsDefined(status)) return BadRequest();
        var result = await sender.Send(new UpdateVeterinarianAppointmentStatusCommand(clinicId, userId, appointmentId, status), ct);
        return result.IsSuccess ? NoContent() : UnprocessableEntity(result.Errors);
    }

    [HttpPatch("me/appointments/{appointmentId:guid}/status")]
    [Authorize(Roles = "Clinic")]
    [EnableRateLimiting("public-api")]
    public async Task<IActionResult> UpdateVeterinarianAppointmentStatus(
        Guid appointmentId,
        [FromBody] UpdateVeterinarianAppointmentStatusRequest request,
        CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var clinicResult = await sender.Send(new GetMyClinicQuery(userId), ct);
        if (clinicResult.IsFailure || clinicResult.Value is null) return Forbid();
        if (!Enum.TryParse<PawTrack.Domain.Certificates.VeterinarianAppointmentStatus>(request.Status, ignoreCase: true, out var status))
            return BadRequest(new ProblemDetails { Detail = "Estado de cita inválido.", Status = 400 });

        var result = await sender.Send(new UpdateVeterinarianAppointmentStatusCommand(
            clinicResult.Value.Id, userId, appointmentId, status), ct);

        return result.IsSuccess ? NoContent() : UnprocessableEntity(result.Errors);
    }

    [HttpPatch("me/appointments/{appointmentId:guid}/time")]
    [Authorize(Roles = "Clinic")]
    [EnableRateLimiting("public-api")]
    public async Task<IActionResult> RescheduleVeterinarianAppointment(
        Guid appointmentId,
        [FromBody] RescheduleVeterinarianAppointmentRequest request,
        CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var clinicResult = await sender.Send(new GetMyClinicQuery(userId), ct);
        if (clinicResult.IsFailure || clinicResult.Value is null) return Forbid();

        var result = await sender.Send(new RescheduleVeterinarianAppointmentCommand(
            clinicResult.Value.Id,
            userId,
            appointmentId,
            request.StartsAt,
            request.DurationMinutes), ct);

        return result.IsSuccess ? NoContent() : UnprocessableEntity(result.Errors);
    }

    [HttpPost("me/appointments/{appointmentId:guid}/consultation")]
    [Authorize(Roles = "Clinic")]
    [EnableRateLimiting("public-api")]
    public async Task<IActionResult> CreateClinicalConsultation(
        Guid appointmentId,
        [FromBody] CreateClinicalConsultationRequest request,
        CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var clinicResult = await sender.Send(new GetMyClinicQuery(userId), ct);
        if (clinicResult.IsFailure || clinicResult.Value is null) return Forbid();

        var result = await sender.Send(new CreateClinicalConsultationCommand(
            clinicResult.Value.Id,
            userId,
            appointmentId,
            request.Reason,
            request.Subjective,
            request.Objective,
            request.Assessment,
            request.Plan,
            request.WeightKg,
            request.TemperatureC,
            request.HeartRateBpm,
            request.RespiratoryRateRpm,
            request.BodyConditionScore,
            request.PainScore,
            request.HydrationStatus,
            request.Diagnosis,
            request.Treatment,
            request.OwnerSummary,
            request.PrescriptionInstructions), ct);

        return result.IsSuccess ? Created(string.Empty, result.Value) : UnprocessableEntity(result.Errors);
    }

    [HttpPost("{clinicId:guid}/staff/appointments/{appointmentId:guid}/consultation")]
    [Authorize]
    [EnableRateLimiting("public-api")]
    public async Task<IActionResult> CreateStaffConsultation(Guid clinicId, Guid appointmentId,
        [FromBody] CreateClinicalConsultationRequest request, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await sender.Send(new CreateClinicalConsultationCommand(clinicId, userId, appointmentId,
            request.Reason, request.Subjective, request.Objective, request.Assessment, request.Plan,
            request.WeightKg, request.TemperatureC, request.HeartRateBpm, request.RespiratoryRateRpm,
            request.BodyConditionScore, request.PainScore, request.HydrationStatus, request.Diagnosis,
            request.Treatment, request.OwnerSummary, request.PrescriptionInstructions), ct);
        return result.IsSuccess ? Created(string.Empty, result.Value) : UnprocessableEntity(result.Errors);
    }

    [HttpPost("{clinicId:guid}/staff/consultations/{consultationId:guid}/close")]
    [Authorize]
    [EnableRateLimiting("public-api")]
    public async Task<IActionResult> CloseStaffConsultation(Guid clinicId, Guid consultationId,
        [FromBody] CloseClinicalConsultationRequest request, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await sender.Send(new CloseClinicalConsultationCommand(clinicId, userId, consultationId,
            request.SignedByName, request.InventoryUses), ct);
        return result.IsSuccess ? Ok(result.Value) : UnprocessableEntity(result.Errors);
    }

    [HttpGet("me/consultation-templates")]
    [Authorize(Roles = "Clinic")]
    [EnableRateLimiting("public-api")]
    public async Task<IActionResult> GetClinicalConsultationTemplates(CancellationToken ct)
    {
        if (!TryGetUserId(out _)) return Unauthorized();
        var result = await sender.Send(new GetClinicalConsultationTemplatesQuery(), ct);
        return result.IsSuccess ? Ok(result.Value) : UnprocessableEntity(result.Errors);
    }

    [HttpGet("me/consultations/{consultationId:guid}/prescription")]
    [Authorize(Roles = "Clinic")]
    [EnableRateLimiting("public-api")]
    public async Task<IActionResult> DownloadClinicalConsultationPrescription(Guid consultationId, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var clinicResult = await sender.Send(new GetMyClinicQuery(userId), ct);
        if (clinicResult.IsFailure || clinicResult.Value is null) return Forbid();

        var result = await sender.Send(new DownloadClinicalConsultationPrescriptionQuery(
            clinicResult.Value.Id,
            userId,
            consultationId), ct);
        return result.IsSuccess
            ? File(result.Value!, "text/plain", $"consulta-{consultationId}-indicaciones.txt")
            : UnprocessableEntity(result.Errors);
    }

    [HttpPost("me/consultations/{consultationId:guid}/attachment")]
    [Authorize(Roles = "Clinic")]
    [Consumes("multipart/form-data")]
    [EnableRateLimiting("public-api")]
    [RequestSizeLimit(5_242_880)]
    public async Task<IActionResult> UploadClinicalConsultationAttachment(
        Guid consultationId,
        IFormFile file,
        CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var clinicResult = await sender.Send(new GetMyClinicQuery(userId), ct);
        if (clinicResult.IsFailure || clinicResult.Value is null) return Forbid();
        if (file.Length <= 0) return BadRequest(new ProblemDetails { Detail = "Archivo requerido.", Status = 400 });
        var allowed = new[] { "application/pdf", "image/jpeg", "image/png" };
        if (!allowed.Contains(file.ContentType, StringComparer.OrdinalIgnoreCase))
            return BadRequest(new ProblemDetails { Detail = "Solo se aceptan PDF, JPEG o PNG.", Status = 400 });

        using var ms = new MemoryStream();
        await file.CopyToAsync(ms, ct);
        var result = await sender.Send(new UploadClinicalConsultationAttachmentCommand(
            clinicResult.Value.Id,
            userId,
            consultationId,
            ms.ToArray(),
            file.ContentType), ct);

        return result.IsSuccess ? Ok(new { attachmentUrl = result.Value }) : UnprocessableEntity(result.Errors);
    }

    [HttpPost("me/consultations/{consultationId:guid}/close")]
    [Authorize(Roles = "Clinic")]
    [EnableRateLimiting("public-api")]
    public async Task<IActionResult> CloseClinicalConsultation(
        Guid consultationId,
        [FromBody] CloseClinicalConsultationRequest request,
        CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var clinicResult = await sender.Send(new GetMyClinicQuery(userId), ct);
        if (clinicResult.IsFailure || clinicResult.Value is null) return Forbid();

        var result = await sender.Send(new CloseClinicalConsultationCommand(
            clinicResult.Value.Id,
            userId,
            consultationId,
            request.SignedByName,
            request.InventoryUses), ct);

        return result.IsSuccess ? Ok(result.Value) : UnprocessableEntity(result.Errors);
    }

    [HttpGet("me/inventory")]
    [Authorize(Roles = "Clinic")]
    [EnableRateLimiting("public-api")]
    public async Task<IActionResult> GetClinicInventory(CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var clinicResult = await sender.Send(new GetMyClinicQuery(userId), ct);
        if (clinicResult.IsFailure || clinicResult.Value is null) return Forbid();
        var result = await sender.Send(new GetClinicInventoryQuery(clinicResult.Value.Id, userId), ct);
        return result.IsSuccess ? Ok(result.Value) : UnprocessableEntity(result.Errors);
    }

    [HttpGet("me/inventory/valuation")]
    [Authorize(Roles = "Clinic")]
    [EnableRateLimiting("public-api")]
    public async Task<IActionResult> GetClinicInventoryValuation(CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var clinicResult = await sender.Send(new GetMyClinicQuery(userId), ct);
        if (clinicResult.IsFailure || clinicResult.Value is null) return Forbid();
        var result = await sender.Send(new GetClinicInventoryValuationQuery(clinicResult.Value.Id, userId), ct);
        return result.IsSuccess ? Ok(result.Value) : UnprocessableEntity(result.Errors);
    }

    [HttpPost("me/inventory/items")]
    [Authorize(Roles = "Clinic")]
    [EnableRateLimiting("public-api")]
    public async Task<IActionResult> AddClinicInventoryItem([FromBody] AddClinicInventoryItemRequest request, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var clinicResult = await sender.Send(new GetMyClinicQuery(userId), ct);
        if (clinicResult.IsFailure || clinicResult.Value is null) return Forbid();
        if (!Enum.TryParse<ClinicInventoryItemType>(request.Type, ignoreCase: true, out var type))
            return BadRequest(new ProblemDetails { Detail = "Tipo de inventario inválido.", Status = 400 });
        var result = await sender.Send(new AddClinicInventoryItemCommand(clinicResult.Value.Id, userId, request.Name, type, request.Unit, request.MinimumStock), ct);
        return result.IsSuccess ? Created(string.Empty, result.Value) : UnprocessableEntity(result.Errors);
    }

    [HttpPost("me/inventory/items/{itemId:guid}/lots")]
    [Authorize(Roles = "Clinic")]
    [EnableRateLimiting("public-api")]
    public async Task<IActionResult> ReceiveClinicInventoryLot(Guid itemId, [FromBody] ReceiveClinicInventoryLotRequest request, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var clinicResult = await sender.Send(new GetMyClinicQuery(userId), ct);
        if (clinicResult.IsFailure || clinicResult.Value is null) return Forbid();
        var result = await sender.Send(new ReceiveClinicInventoryLotCommand(clinicResult.Value.Id, userId, itemId, request.LotNumber, request.ExpiresAt, request.Quantity, request.UnitCostCrc, request.SupplierName, request.LocationName), ct);
        return result.IsSuccess ? Created(string.Empty, result.Value) : UnprocessableEntity(result.Errors);
    }

    [HttpPost("me/inventory/lots/{lotId:guid}/adjustments")]
    [Authorize(Roles = "Clinic")]
    [EnableRateLimiting("public-api")]
    public async Task<IActionResult> AdjustClinicInventoryLot(Guid lotId, [FromBody] AdjustClinicInventoryLotRequest request, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var clinicResult = await sender.Send(new GetMyClinicQuery(userId), ct);
        if (clinicResult.IsFailure || clinicResult.Value is null) return Forbid();
        var result = await sender.Send(new AdjustClinicInventoryLotCommand(clinicResult.Value.Id, userId, lotId, request.QuantityDelta, request.Reason), ct);
        return result.IsSuccess ? Ok(result.Value) : UnprocessableEntity(result.Errors);
    }

    [HttpPost("me/sales")]
    [Authorize(Roles = "Clinic")]
    [EnableRateLimiting("public-api")]
    public async Task<IActionResult> CreateClinicSale([FromBody] CreateClinicSaleRequest request, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var clinicResult = await sender.Send(new GetMyClinicQuery(userId), ct);
        if (clinicResult.IsFailure || clinicResult.Value is null) return Forbid();
        var result = await sender.Send(new CreateClinicSaleCommand(clinicResult.Value.Id, userId, request.AppointmentId, request.ConsultationId, request.PetId, request.ReceiptNumber, request.Lines, request.DiscountCrc, request.DiscountReason), ct);
        return result.IsSuccess ? Created(string.Empty, result.Value) : UnprocessableEntity(result.Errors);
    }

    [HttpPost("me/sales/{saleId:guid}/payments")]
    [Authorize(Roles = "Clinic")]
    [EnableRateLimiting("public-api")]
    public async Task<IActionResult> RegisterClinicSalePayment(Guid saleId, [FromBody] RegisterClinicSalePaymentRequest request, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var clinicResult = await sender.Send(new GetMyClinicQuery(userId), ct);
        if (clinicResult.IsFailure || clinicResult.Value is null) return Forbid();
        if (!Enum.TryParse<ClinicPaymentMethod>(request.Method, ignoreCase: true, out var method))
            return BadRequest(new ProblemDetails { Detail = "Método de pago inválido.", Status = 400 });
        var result = await sender.Send(new RegisterClinicSalePaymentCommand(clinicResult.Value.Id, userId, saleId, request.AmountCrc, method, request.Reference), ct);
        return result.IsSuccess ? Ok(result.Value) : UnprocessableEntity(result.Errors);
    }

    [HttpGet("me/sales/{saleId:guid}/ledger")]
    [Authorize(Roles = "Clinic")]
    [EnableRateLimiting("public-api")]
    public async Task<IActionResult> GetMySaleLedger(Guid saleId, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var clinic = await sender.Send(new GetMyClinicQuery(userId), ct);
        if (clinic.IsFailure || clinic.Value is null) return Forbid();
        var result = await sender.Send(new GetClinicSaleLedgerQuery(clinic.Value.Id, userId, saleId), ct);
        return result.IsSuccess ? Ok(result.Value) : NotFound();
    }

    [HttpPost("me/sales/{saleId:guid}/refunds")]
    [Authorize(Roles = "Clinic", Policy = "ClinicFinanceMfa")]
    [EnableRateLimiting("public-api")]
    public async Task<IActionResult> RecordMySaleRefund(Guid saleId, [FromBody] RecordClinicSaleRefundRequest request, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var clinic = await sender.Send(new GetMyClinicQuery(userId), ct);
        if (clinic.IsFailure || clinic.Value is null) return Forbid();
        var result = await sender.Send(new RecordClinicSaleRefundCommand(clinic.Value.Id, userId, saleId, request.PaymentId, request.AmountCrc, request.Reason, request.EvidenceReference), ct);
        return result.IsSuccess ? Ok(result.Value) : UnprocessableEntity(result.Errors);
    }

    [HttpPost("me/sales/{saleId:guid}/void")]
    [Authorize(Roles = "Clinic", Policy = "ClinicFinanceMfa")]
    [EnableRateLimiting("public-api")]
    public async Task<IActionResult> VoidClinicSale(Guid saleId, [FromBody] ReasonRequest request, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var clinicResult = await sender.Send(new GetMyClinicQuery(userId), ct);
        if (clinicResult.IsFailure || clinicResult.Value is null) return Forbid();
        var result = await sender.Send(new VoidClinicSaleCommand(clinicResult.Value.Id, userId, saleId, request.Reason), ct);
        return result.IsSuccess ? Ok(result.Value) : UnprocessableEntity(result.Errors);
    }

    [HttpPost("me/cash-closes")]
    [Authorize(Roles = "Clinic", Policy = "ClinicFinanceMfa")]
    [EnableRateLimiting("public-api")]
    public async Task<IActionResult> CloseClinicCash([FromBody] CloseClinicCashRequest request, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var clinicResult = await sender.Send(new GetMyClinicQuery(userId), ct);
        if (clinicResult.IsFailure || clinicResult.Value is null) return Forbid();
        var result = await sender.Send(new CloseClinicCashCommand(clinicResult.Value.Id, userId, request.BusinessDate), ct);
        return result.IsSuccess ? Created(string.Empty, new { cashCloseId = result.Value }) : UnprocessableEntity(result.Errors);
    }

    [HttpPost("me/sales/{saleId:guid}/fiscal-submission")]
    [Authorize(Roles = "Clinic", Policy = "ClinicFinanceMfa")]
    [EnableRateLimiting("public-api")]
    public async Task<IActionResult> SubmitMyFiscalSale(Guid saleId, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var clinic = await sender.Send(new GetMyClinicQuery(userId), ct);
        if (clinic.IsFailure || clinic.Value is null) return Forbid();
        var result = await sender.Send(new SubmitClinicSaleFiscalCommand(clinic.Value.Id, userId, saleId), ct);
        return result.IsSuccess ? Accepted(result.Value) : UnprocessableEntity(result.Errors);
    }

    [HttpGet("me/sales-report")]
    [Authorize(Roles = "Clinic")]
    [EnableRateLimiting("public-api")]
    public async Task<IActionResult> GetClinicSalesReport([FromQuery] DateOnly businessDate, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var clinicResult = await sender.Send(new GetMyClinicQuery(userId), ct);
        if (clinicResult.IsFailure || clinicResult.Value is null) return Forbid();
        var result = await sender.Send(new GetClinicSalesReportQuery(clinicResult.Value.Id, userId, businessDate), ct);
        return result.IsSuccess ? Ok(result.Value) : UnprocessableEntity(result.Errors);
    }

    [HttpGet("me/finance/members")]
    [Authorize(Roles = "Clinic")]
    [EnableRateLimiting("public-api")]
    public async Task<IActionResult> GetFinanceMembers(CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var clinic = await sender.Send(new GetMyClinicQuery(userId), ct);
        if (clinic.IsFailure || clinic.Value is null) return Forbid();
        var result = await sender.Send(new GetClinicFinanceMembershipsQuery(clinic.Value.Id, userId), ct);
        return result.IsSuccess ? Ok(result.Value) : Forbid();
    }

    [HttpGet("finance-workspaces")]
    [Authorize]
    [EnableRateLimiting("public-api")]
    public async Task<IActionResult> GetMyFinanceWorkspaces(CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        return Ok(await sender.Send(new GetMyClinicFinanceWorkspacesQuery(userId), ct));
    }

    [HttpPut("me/finance/members")]
    [Authorize(Roles = "Clinic", Policy = "ClinicFinanceMfa")]
    [EnableRateLimiting("public-api")]
    public async Task<IActionResult> GrantFinanceMember([FromBody] GrantClinicFinanceMemberRequest request, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var clinic = await sender.Send(new GetMyClinicQuery(userId), ct);
        if (clinic.IsFailure || clinic.Value is null) return Forbid();
        if (!Enum.TryParse<ClinicFinanceRole>(request.Role, true, out var role) || !Enum.IsDefined(role)) return BadRequest();
        var result = await sender.Send(new GrantClinicFinanceMembershipCommand(clinic.Value.Id, userId, request.Email, role), ct);
        return result.IsSuccess ? Ok(new { membershipId = result.Value }) : UnprocessableEntity(result.Errors);
    }

    [HttpDelete("me/finance/members/{memberUserId:guid}")]
    [Authorize(Roles = "Clinic", Policy = "ClinicFinanceMfa")]
    [EnableRateLimiting("public-api")]
    public async Task<IActionResult> RevokeFinanceMember(Guid memberUserId, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var clinic = await sender.Send(new GetMyClinicQuery(userId), ct);
        if (clinic.IsFailure || clinic.Value is null) return Forbid();
        var result = await sender.Send(new RevokeClinicFinanceMembershipCommand(clinic.Value.Id, userId, memberUserId), ct);
        return result.IsSuccess ? NoContent() : UnprocessableEntity(result.Errors);
    }

    [HttpPost("{clinicId:guid}/finance/sales")]
    [Authorize]
    [EnableRateLimiting("public-api")]
    public async Task<IActionResult> CreateFinanceSale(Guid clinicId, [FromBody] CreateClinicSaleRequest request, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await sender.Send(new CreateClinicSaleCommand(clinicId, userId, request.AppointmentId, request.ConsultationId, request.PetId, request.ReceiptNumber, request.Lines, request.DiscountCrc, request.DiscountReason), ct);
        return result.IsSuccess ? Created(string.Empty, result.Value) : UnprocessableEntity(result.Errors);
    }

    [HttpPost("{clinicId:guid}/finance/sales/{saleId:guid}/payments")]
    [Authorize]
    [EnableRateLimiting("public-api")]
    public async Task<IActionResult> RegisterFinancePayment(Guid clinicId, Guid saleId, [FromBody] RegisterClinicSalePaymentRequest request, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        if (!Enum.TryParse<ClinicPaymentMethod>(request.Method, true, out var method) || !Enum.IsDefined(method)) return BadRequest();
        var result = await sender.Send(new RegisterClinicSalePaymentCommand(clinicId, userId, saleId, request.AmountCrc, method, request.Reference), ct);
        return result.IsSuccess ? Ok(result.Value) : UnprocessableEntity(result.Errors);
    }

    [HttpGet("{clinicId:guid}/finance/sales/{saleId:guid}/ledger")]
    [Authorize]
    [EnableRateLimiting("public-api")]
    public async Task<IActionResult> GetFinanceSaleLedger(Guid clinicId, Guid saleId, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await sender.Send(new GetClinicSaleLedgerQuery(clinicId, userId, saleId), ct);
        return result.IsSuccess ? Ok(result.Value) : NotFound();
    }

    [HttpPost("{clinicId:guid}/finance/sales/{saleId:guid}/refunds")]
    [Authorize(Policy = "ClinicFinanceMfa")]
    [EnableRateLimiting("public-api")]
    public async Task<IActionResult> RecordFinanceRefund(Guid clinicId, Guid saleId, [FromBody] RecordClinicSaleRefundRequest request, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await sender.Send(new RecordClinicSaleRefundCommand(clinicId, userId, saleId, request.PaymentId, request.AmountCrc, request.Reason, request.EvidenceReference), ct);
        return result.IsSuccess ? Ok(result.Value) : UnprocessableEntity(result.Errors);
    }

    [HttpPost("{clinicId:guid}/finance/sales/{saleId:guid}/void")]
    [Authorize(Policy = "ClinicFinanceMfa")]
    [EnableRateLimiting("public-api")]
    public async Task<IActionResult> VoidFinanceSale(Guid clinicId, Guid saleId, [FromBody] ReasonRequest request, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await sender.Send(new VoidClinicSaleCommand(clinicId, userId, saleId, request.Reason), ct);
        return result.IsSuccess ? Ok(result.Value) : UnprocessableEntity(result.Errors);
    }

    [HttpPost("{clinicId:guid}/finance/cash-closes")]
    [Authorize(Policy = "ClinicFinanceMfa")]
    [EnableRateLimiting("public-api")]
    public async Task<IActionResult> CloseFinanceCash(Guid clinicId, [FromBody] CloseClinicCashRequest request, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await sender.Send(new CloseClinicCashCommand(clinicId, userId, request.BusinessDate), ct);
        return result.IsSuccess ? Created(string.Empty, new { cashCloseId = result.Value }) : UnprocessableEntity(result.Errors);
    }

    [HttpPost("{clinicId:guid}/finance/sales/{saleId:guid}/fiscal-submission")]
    [Authorize(Policy = "ClinicFinanceMfa")]
    [EnableRateLimiting("public-api")]
    public async Task<IActionResult> SubmitFinanceFiscalSale(Guid clinicId, Guid saleId, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await sender.Send(new SubmitClinicSaleFiscalCommand(clinicId, userId, saleId), ct);
        return result.IsSuccess ? Accepted(result.Value) : UnprocessableEntity(result.Errors);
    }

    [HttpGet("{clinicId:guid}/finance/sales-report")]
    [Authorize]
    [EnableRateLimiting("public-api")]
    public async Task<IActionResult> GetFinanceReport(Guid clinicId, [FromQuery] DateOnly businessDate, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await sender.Send(new GetClinicSalesReportQuery(clinicId, userId, businessDate), ct);
        return result.IsSuccess ? Ok(result.Value) : UnprocessableEntity(result.Errors);
    }

    [HttpGet("me/crm-dashboard")]
    [Authorize(Roles = "Clinic")]
    [EnableRateLimiting("public-api")]
    public async Task<IActionResult> GetClinicCrmDashboard([FromQuery] DateOnly? today, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var clinicResult = await sender.Send(new GetMyClinicQuery(userId), ct);
        if (clinicResult.IsFailure || clinicResult.Value is null) return Forbid();
        var result = await sender.Send(new GetClinicCrmDashboardQuery(clinicResult.Value.Id, userId, today ?? DateOnly.FromDateTime(DateTime.UtcNow)), ct);
        return result.IsSuccess ? Ok(result.Value) : UnprocessableEntity(result.Errors);
    }

    [HttpGet("me/crm/templates")]
    [Authorize(Roles = "Clinic")]
    [EnableRateLimiting("public-api")]
    public IActionResult GetClinicCommunicationTemplates() => Ok(ClinicCommunicationTemplates.All);

    [HttpPost("me/crm/send-template")]
    [Authorize(Roles = "Clinic")]
    [EnableRateLimiting("public-api")]
    public async Task<IActionResult> SendClinicCommunicationTemplate([FromBody] SendClinicCommunicationTemplateRequest request, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var clinic = await sender.Send(new GetMyClinicQuery(userId), ct);
        if (clinic.IsFailure || clinic.Value is null) return Forbid();
        if (!Enum.TryParse<ClinicCommunicationChannel>(request.Channel, true, out var channel) || !Enum.IsDefined(channel)) return BadRequest();
        var result = await sender.Send(new SendClinicCommunicationTemplateCommand(clinic.Value.Id, userId,
            request.PetId, request.RequestId, request.TemplateKey, channel), ct);
        return result.IsSuccess ? Accepted(new { activityId = result.Value }) : UnprocessableEntity(result.Errors);
    }

    [HttpPut("me/crm/preferences")]
    [Authorize(Roles = "Clinic")]
    [EnableRateLimiting("public-api")]
    public async Task<IActionResult> UpsertClinicCommunicationPreference([FromBody] UpsertClinicCommunicationPreferenceRequest request, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var clinicResult = await sender.Send(new GetMyClinicQuery(userId), ct);
        if (clinicResult.IsFailure || clinicResult.Value is null) return Forbid();
        if (!Enum.TryParse<ClinicCommunicationChannel>(request.Channel, ignoreCase: true, out var channel))
            return BadRequest(new ProblemDetails { Detail = "Canal inválido.", Status = 400 });
        if (!Enum.TryParse<ClinicCommunicationPurpose>(request.Purpose, ignoreCase: true, out var purpose))
            return BadRequest(new ProblemDetails { Detail = "Propósito inválido.", Status = 400 });
        var result = await sender.Send(new UpsertClinicCommunicationPreferenceCommand(clinicResult.Value.Id, userId, request.PetId, channel, purpose, request.IsOptedIn, request.ConsentSource), ct);
        return result.IsSuccess ? NoContent() : UnprocessableEntity(result.Errors);
    }

    [HttpPut("{clinicId:guid}/pets/{petId:guid}/communication-preferences")]
    [Authorize]
    [EnableRateLimiting("public-api")]
    public async Task<IActionResult> SetOwnerClinicCommunicationPreference(Guid clinicId, Guid petId, [FromBody] SetOwnerClinicCommunicationPreferenceRequest request, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        if (!Enum.TryParse<ClinicCommunicationChannel>(request.Channel, true, out var channel)
            || !Enum.TryParse<ClinicCommunicationPurpose>(request.Purpose, true, out var purpose))
            return BadRequest(new ProblemDetails { Detail = "Canal o propósito inválido.", Status = 400 });
        var result = await sender.Send(new SetOwnerClinicCommunicationPreferenceCommand(clinicId, userId, petId, channel, purpose, request.IsOptedIn), ct);
        return result.IsSuccess ? NoContent() : UnprocessableEntity(result.Errors);
    }

    [HttpGet("pets/{petId:guid}/communication-preferences")]
    [Authorize]
    [EnableRateLimiting("public-api")]
    public async Task<IActionResult> GetOwnerClinicCommunicationPreferences(Guid petId, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await sender.Send(new GetOwnerClinicCommunicationPreferencesQuery(userId, petId), ct);
        return result.IsSuccess ? Ok(result.Value) : NotFound();
    }

    [HttpPost("me/crm/activities")]
    [Authorize(Roles = "Clinic")]
    [EnableRateLimiting("public-api")]
    public async Task<IActionResult> LogClinicCommunicationActivity([FromBody] LogClinicCommunicationActivityRequest request, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var clinicResult = await sender.Send(new GetMyClinicQuery(userId), ct);
        if (clinicResult.IsFailure || clinicResult.Value is null) return Forbid();
        if (!Enum.TryParse<ClinicCommunicationChannel>(request.Channel, ignoreCase: true, out var channel))
            return BadRequest(new ProblemDetails { Detail = "Canal inválido.", Status = 400 });
        if (!Enum.TryParse<ClinicCommunicationPurpose>(request.Purpose, ignoreCase: true, out var purpose))
            return BadRequest(new ProblemDetails { Detail = "Propósito inválido.", Status = 400 });
        if (!Enum.TryParse<ClinicCommunicationDirection>(request.Direction, ignoreCase: true, out var direction))
            return BadRequest(new ProblemDetails { Detail = "Dirección inválida.", Status = 400 });
        if (!Enum.TryParse<ClinicCommunicationStatus>(request.Status, ignoreCase: true, out var status))
            return BadRequest(new ProblemDetails { Detail = "Estado inválido.", Status = 400 });
        var result = await sender.Send(new LogClinicCommunicationActivityCommand(clinicResult.Value.Id, userId, request.PetId, channel, purpose, direction, status, request.Subject, request.Body, request.ProviderMessageId), ct);
        return result.IsSuccess ? Accepted() : UnprocessableEntity(result.Errors);
    }

    [HttpPost("me/crm/tasks")]
    [Authorize(Roles = "Clinic")]
    [EnableRateLimiting("public-api")]
    public async Task<IActionResult> CreateClinicCrmTask([FromBody] CreateClinicCrmTaskRequest request, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var clinicResult = await sender.Send(new GetMyClinicQuery(userId), ct);
        if (clinicResult.IsFailure || clinicResult.Value is null) return Forbid();
        if (!Enum.TryParse<ClinicCrmTaskType>(request.Type, ignoreCase: true, out var type))
            return BadRequest(new ProblemDetails { Detail = "Tipo de tarea inválido.", Status = 400 });
        var result = await sender.Send(new CreateClinicCrmTaskCommand(clinicResult.Value.Id, userId, request.PetId, type, request.DueDate, request.Title, request.Notes), ct);
        return result.IsSuccess ? Created(string.Empty, new { taskId = result.Value }) : UnprocessableEntity(result.Errors);
    }

    [HttpPost("me/crm/tasks/{taskId:guid}/complete")]
    [Authorize(Roles = "Clinic")]
    [EnableRateLimiting("public-api")]
    public async Task<IActionResult> CompleteClinicCrmTask(Guid taskId, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var clinicResult = await sender.Send(new GetMyClinicQuery(userId), ct);
        if (clinicResult.IsFailure || clinicResult.Value is null) return Forbid();
        var result = await sender.Send(new CompleteClinicCrmTaskCommand(clinicResult.Value.Id, userId, taskId), ct);
        return result.IsSuccess ? NoContent() : UnprocessableEntity(result.Errors);
    }

    [HttpPut("me/veterinarians/{veterinarianId:guid}/permissions")]
    [Authorize(Roles = "Clinic")]
    [EnableRateLimiting("public-api")]
    public async Task<IActionResult> SetVeterinarianPermissions(
        Guid veterinarianId,
        [FromBody] SetVeterinarianPermissionsRequest request,
        CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var clinicResult = await sender.Send(new GetMyClinicQuery(userId), ct);
        if (clinicResult.IsFailure || clinicResult.Value is null) return Forbid();
        var result = await sender.Send(new SetVeterinarianPermissionsCommand(
            clinicResult.Value.Id, userId, veterinarianId, request.Permissions), ct);
        return result.IsSuccess ? NoContent() : UnprocessableEntity(result.Errors);
    }

    [HttpPost("patients/{petId:guid}/medical/export")]
    [Authorize(Roles = "Clinic")]
    [EnableRateLimiting("public-api")]
    public async Task<IActionResult> ExportPatientMedical(Guid petId, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var clinicResult = await sender.Send(new GetMyClinicQuery(userId), ct);
        if (clinicResult.IsFailure || clinicResult.Value is null) return Forbid();
        var result = await sender.Send(new ExportClinicMedicalCommand(clinicResult.Value.Id, userId, petId), ct);
        return result.IsSuccess ? Accepted(result.Value) : UnprocessableEntity(result.Errors);
    }

    [HttpGet("medical-exports/{exportId:guid}/download")]
    [Authorize(Roles = "Clinic")]
    [EnableRateLimiting("public-api")]
    public async Task<IActionResult> DownloadPatientMedicalExport(
        Guid exportId,
        [FromServices] IClinicMedicalExportRepository exportRepository,
        [FromServices] IBlobStorageService blobStorage,
        CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var export = await exportRepository.GetByIdAsync(exportId, ct);
        if (export is null || !export.IsDownloadable) return NotFound();
        var clinic = await sender.Send(new GetMyClinicQuery(userId), ct);
        if (clinic.IsFailure || clinic.Value is null || clinic.Value.Id != export.ClinicId) return Forbid();
        var bytes = await blobStorage.DownloadAsync(export.BlobUrl, ct);
        return bytes is null ? NotFound() : File(bytes, "application/pdf", $"expediente-{export.PetId}.pdf");
    }

    [HttpGet("me/verification")]
    [Authorize(Roles = "Clinic")]
    [EnableRateLimiting("public-api")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyVerification(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var clinicResult = await sender.Send(new GetMyClinicQuery(userId), cancellationToken);
        if (clinicResult.IsFailure || clinicResult.Value is null) return Forbid();

        var result = await sender.Send(new GetMyClinicVerificationQuery(clinicResult.Value.Id, userId), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Errors);
    }

    [HttpPost("me/verification")]
    [Authorize(Roles = "Clinic")]
    [EnableRateLimiting("public-api")]
    [RequestSizeLimit(128)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> SubmitMyVerification(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var clinicResult = await sender.Send(new GetMyClinicQuery(userId), cancellationToken);
        if (clinicResult.IsFailure || clinicResult.Value is null) return Forbid();

        var result = await sender.Send(new SubmitClinicVerificationCommand(clinicResult.Value.Id, userId), cancellationToken);
        return result.IsSuccess ? Created(string.Empty, result.Value)
            : UnprocessableEntity(new ProblemDetails { Detail = string.Join("; ", result.Errors), Status = 422 });
    }

    [HttpPost("me/verification/document")]
    [Authorize(Roles = "Clinic")]
    [Consumes("multipart/form-data")]
    [EnableRateLimiting("public-api")]
    [RequestSizeLimit(5_242_880)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> UploadMyVerificationDocument(IFormFile file, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var clinicResult = await sender.Send(new GetMyClinicQuery(userId), cancellationToken);
        if (clinicResult.IsFailure || clinicResult.Value is null) return Forbid();

        using var ms = new MemoryStream();
        await file.CopyToAsync(ms, cancellationToken);
        var result = await sender.Send(new UploadClinicVerificationDocumentCommand(
            clinicResult.Value.Id, userId, ms.ToArray(), file.ContentType), cancellationToken);

        return result.IsSuccess ? Ok(result.Value)
            : UnprocessableEntity(new ProblemDetails { Detail = string.Join("; ", result.Errors), Status = 422 });
    }

    [HttpGet("me/verification/document")]
    [Authorize(Roles = "Clinic")]
    [EnableRateLimiting("public-api")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> DownloadMyVerificationDocument(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var clinicResult = await sender.Send(new GetMyClinicQuery(userId), cancellationToken);
        if (clinicResult.IsFailure || clinicResult.Value is null) return Forbid();

        var verification = await sender.Send(new GetMyClinicVerificationQuery(clinicResult.Value.Id, userId), cancellationToken);
        if (verification.IsFailure || verification.Value is null)
            return NotFound(new ProblemDetails { Detail = "Documento no disponible.", Status = 404 });

        var result = await sender.Send(new DownloadClinicVerificationDocumentQuery(verification.Value.Id, userId, IsAdmin: false), cancellationToken);
        return result.IsSuccess
            ? File(result.Value!.Bytes, result.Value.ContentType, result.Value.FileName)
            : UnprocessableEntity(new ProblemDetails { Detail = string.Join("; ", result.Errors), Status = 422 });
    }

    [HttpGet("me/veterinarians")]
    [Authorize(Roles = "Clinic")]
    [EnableRateLimiting("public-api")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyVeterinarians(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var clinicResult = await sender.Send(new GetMyClinicQuery(userId), cancellationToken);
        if (clinicResult.IsFailure || clinicResult.Value is null) return Forbid();

        var result = await sender.Send(new GetMyClinicVeterinariansQuery(clinicResult.Value.Id, userId), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Errors);
    }

    [HttpPost("me/veterinarians")]
    [Authorize(Roles = "Clinic")]
    [EnableRateLimiting("public-api")]
    [RequestSizeLimit(512)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> CreateVeterinarian(
        [FromBody] CreateClinicVeterinarianRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var clinicResult = await sender.Send(new GetMyClinicQuery(userId), cancellationToken);
        if (clinicResult.IsFailure || clinicResult.Value is null) return Forbid();

        var result = await sender.Send(
            new CreateClinicVeterinarianCommand(
                clinicResult.Value.Id,
                userId,
                request.FullName,
                request.LicenseNumber),
            cancellationToken);

        if (result.IsFailure)
            return UnprocessableEntity(new ProblemDetails { Detail = string.Join("; ", result.Errors), Status = 422 });

        return Created(string.Empty, result.Value);
    }

    [HttpPost("me/veterinarians/{veterinarianId:guid}/document")]
    [Authorize(Roles = "Clinic")]
    [Consumes("multipart/form-data")]
    [EnableRateLimiting("public-api")]
    [RequestSizeLimit(5_242_880)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> UploadVeterinarianDocument(Guid veterinarianId, IFormFile file, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var clinicResult = await sender.Send(new GetMyClinicQuery(userId), cancellationToken);
        if (clinicResult.IsFailure || clinicResult.Value is null) return Forbid();

        using var ms = new MemoryStream();
        await file.CopyToAsync(ms, cancellationToken);
        var result = await sender.Send(new UploadVeterinarianDocumentCommand(
            clinicResult.Value.Id, veterinarianId, userId, ms.ToArray(), file.ContentType), cancellationToken);

        return result.IsSuccess ? Ok(result.Value)
            : UnprocessableEntity(new ProblemDetails { Detail = string.Join("; ", result.Errors), Status = 422 });
    }

    [HttpGet("me/veterinarians/{veterinarianId:guid}/document")]
    [Authorize(Roles = "Clinic")]
    [EnableRateLimiting("public-api")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> DownloadVeterinarianDocument(Guid veterinarianId, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await sender.Send(new DownloadVeterinarianDocumentQuery(veterinarianId, userId, IsAdmin: false), cancellationToken);
        return result.IsSuccess
            ? File(result.Value!.Bytes, result.Value.ContentType, result.Value.FileName)
            : UnprocessableEntity(new ProblemDetails { Detail = string.Join("; ", result.Errors), Status = 422 });
    }

    [HttpPost("me/veterinarians/{veterinarianId:guid}/signature")]
    [Authorize(Roles = "Clinic")]
    [Consumes("multipart/form-data")]
    [EnableRateLimiting("public-api")]
    [RequestSizeLimit(2_097_152)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> UploadVeterinarianSignature(Guid veterinarianId, IFormFile file, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var clinicResult = await sender.Send(new GetMyClinicQuery(userId), cancellationToken);
        if (clinicResult.IsFailure || clinicResult.Value is null) return Forbid();

        using var ms = new MemoryStream();
        await file.CopyToAsync(ms, cancellationToken);
        var result = await sender.Send(new UploadVeterinarianSignatureCommand(
            clinicResult.Value.Id, veterinarianId, userId, ms.ToArray(), file.ContentType), cancellationToken);

        return result.IsSuccess ? Ok(result.Value)
            : UnprocessableEntity(new ProblemDetails { Detail = string.Join("; ", result.Errors), Status = 422 });
    }

    [HttpPost("me/veterinarians/{veterinarianId:guid}/revoke")]
    [Authorize(Roles = "Clinic")]
    [EnableRateLimiting("public-api")]
    [RequestSizeLimit(512)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> RevokeMyVeterinarian(
        Guid veterinarianId,
        [FromBody] ReasonRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var clinicResult = await sender.Send(new GetMyClinicQuery(userId), cancellationToken);
        if (clinicResult.IsFailure || clinicResult.Value is null) return Forbid();

        var result = await sender.Send(new RevokeMyClinicVeterinarianCommand(
            clinicResult.Value.Id, veterinarianId, userId, request.Reason), cancellationToken);
        return result.IsSuccess ? Ok(result.Value)
            : UnprocessableEntity(new ProblemDetails { Detail = string.Join("; ", result.Errors), Status = 422 });
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

public sealed record UpdateClinicProfileRequest(
    string Name,
    string Address,
    string? PhoneNumber,
    string? Website,
    bool? IsEmergency24h,
    string? EmergencyPhone,
    string? Description,
    string? Services,
    string? OpeningHours,
    string? WhatsAppNumber = null,
    bool IsWhatsAppContactEnabled = false);

public sealed record ReviewProfileChangeRequest(bool Approve, string? Reason);
public sealed record ScheduleVeterinarianAppointmentRequest(Guid PetId, DateTimeOffset StartsAt, int DurationMinutes);
public sealed record CreateVeterinarianScheduleBlockRequest(DateTimeOffset StartsAt, DateTimeOffset EndsAt, string Reason);
public sealed record UpdateVeterinarianScheduleBlockRequest(DateTimeOffset StartsAt, DateTimeOffset EndsAt, string Reason);
public sealed record UpdateVeterinarianAppointmentStatusRequest(string Status);
public sealed record RescheduleVeterinarianAppointmentRequest(DateTimeOffset StartsAt, int DurationMinutes);
public sealed record CreateClinicalConsultationRequest(
    string Reason,
    string Subjective,
    string Objective,
    string Assessment,
    string Plan,
    decimal? WeightKg,
    decimal? TemperatureC,
    int? HeartRateBpm,
    int? RespiratoryRateRpm,
    int? BodyConditionScore,
    int? PainScore,
    string? HydrationStatus,
    string Diagnosis,
    string Treatment,
    string OwnerSummary,
    string? PrescriptionInstructions = null);
public sealed record CloseClinicalConsultationRequest(string SignedByName, IReadOnlyList<ClinicalInventoryUseInput>? InventoryUses = null);
public sealed record AddClinicInventoryItemRequest(string Name, string Type, string Unit, int MinimumStock);
public sealed record ReceiveClinicInventoryLotRequest(string LotNumber, DateOnly? ExpiresAt, int Quantity, decimal UnitCostCrc, string? SupplierName, string? LocationName = null);
public sealed record AdjustClinicInventoryLotRequest(int QuantityDelta, string Reason);
public sealed record CreateClinicSaleRequest(Guid? AppointmentId, Guid? ConsultationId, Guid? PetId, string ReceiptNumber, IReadOnlyList<ClinicSaleLineInput> Lines, decimal DiscountCrc = 0, string? DiscountReason = null);
public sealed record RegisterClinicSalePaymentRequest(decimal AmountCrc, string Method, string? Reference);
public sealed record RecordClinicSaleRefundRequest(Guid PaymentId, decimal AmountCrc, string Reason, string EvidenceReference);
public sealed record CloseClinicCashRequest(DateOnly BusinessDate);
public sealed record GrantClinicFinanceMemberRequest(string Email, string Role);
public sealed record GrantClinicStaffMemberRequest(string Email, string Role, Guid? VeterinarianId = null);
public sealed record UpsertClinicCommunicationPreferenceRequest(Guid PetId, string Channel, string Purpose, bool IsOptedIn, string ConsentSource);
public sealed record SetOwnerClinicCommunicationPreferenceRequest(string Channel, string Purpose, bool IsOptedIn);
public sealed record LogClinicCommunicationActivityRequest(Guid PetId, string Channel, string Purpose, string Direction, string Status, string Subject, string Body, string? ProviderMessageId = null);
public sealed record CreateClinicCrmTaskRequest(Guid PetId, string Type, DateOnly DueDate, string Title, string? Notes = null);
public sealed record SendClinicCommunicationTemplateRequest(Guid PetId, Guid RequestId, string TemplateKey, string Channel);
public sealed record SetVeterinarianPermissionsRequest(IReadOnlyList<string> Permissions);

public sealed record ClinicScanRequest(
    string Input,
    string InputType);

public sealed record ReviewClinicRequest(bool Approve);
public sealed record VerifyClinicForCertificatesRequest(DateOnly? ExpiresAt);
public sealed record ReviewVerificationRequest(bool Approve, DateOnly? ExpiresAt, string? Reason, string? Notes);
public sealed record ReasonRequest(string Reason);
public sealed record CreateClinicVeterinarianRequest(string FullName, string LicenseNumber);

public sealed record CreateApiKeyRequest(string Label, IReadOnlyList<string>? Scopes = null);

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
public sealed record VerifyMicrochipRequest(string ObservedChipId, string? Notes);
