using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PawTrack.Application.Clinics.Commands.PerformClinicScan;
using PawTrack.Application.Clinics.Commands.RegisterClinic;
using PawTrack.Application.Clinics.Commands.ReviewClinic;
using PawTrack.Application.Clinics.Queries.GetMyClinic;
using PawTrack.Application.Clinics.Queries.GetPendingClinics;
using PawTrack.Domain.Auth;
using PawTrack.Domain.Clinics;
using System.Security.Claims;

namespace PawTrack.API.Controllers;

[ApiController]
[Route("api/clinics")]
public sealed class ClinicsController(ISender sender) : ControllerBase
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
    [EnableRateLimiting("public-api")] // 30/min — each call writes ReviewClinicCommand (DB write)
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
