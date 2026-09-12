using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PawTrack.Application.Clinics.Commands.PerformClinicScan;
using PawTrack.Application.Clinics.Queries.GetMyClinic;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Subscriptions.Interfaces;
using PawTrack.Domain.Clinics;
using PawTrack.Domain.Subscriptions;
using System.Security.Claims;

namespace PawTrack.API.Controllers;

/// <summary>
/// Machine-to-machine pet lookup API for Clínica Partner tier.
/// Dual authentication:
/// 1. M2M CRM integration: Authenticated via X-PawTrack-Key header (ClinicApiKeyMiddleware).
/// 2. Embeddable web widget: Authenticated via X-Widget-Clinic header (verified ClinicPartner subscription).
/// </summary>
[ApiController]
[Route("api/v1/pets")]
[AllowAnonymous]
public sealed class PetLookupController(
    ISender sender,
    IClinicRepository clinicRepository,
    ISubscriptionRepository subscriptionRepository) : ControllerBase
{
    [HttpGet("lookup")]
    [EnableRateLimiting("public-api")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Lookup(
        [FromQuery] string? chip,
        [FromQuery] string? qr,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(chip) && string.IsNullOrWhiteSpace(qr))
            return BadRequest(new ProblemDetails { Detail = "Provide 'chip' or 'qr' query parameter.", Status = 400 });

        Guid clinicId;

        if (User.IsInRole("Clinic"))
        {
            var rawUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(rawUserId, out var userId)) return Unauthorized();

            var clinicResult = await sender.Send(new GetMyClinicQuery(userId), cancellationToken);
            if (clinicResult.IsFailure || clinicResult.Value is null) return Forbid();
            clinicId = clinicResult.Value.Id;
        }
        else if (Request.Headers.TryGetValue("X-Widget-Clinic", out var widgetClinicValue)
                 && Guid.TryParse(widgetClinicValue, out var widgetClinicId))
        {
            var clinic = await clinicRepository.GetByIdAsync(widgetClinicId, cancellationToken);
            if (clinic is null || clinic.Status != ClinicStatus.Active)
                return Forbid();

            var sub = await subscriptionRepository.GetActiveForClinicAsync(widgetClinicId, cancellationToken);
            if (sub is null || sub.Tier < SubscriptionTier.ClinicPartner)
                return Forbid();

            clinicId = widgetClinicId;
        }
        else
        {
            return Unauthorized(new ProblemDetails
            {
                Detail = "Authentication required. Provide X-PawTrack-Key header or valid X-Widget-Clinic identifier.",
                Status = 401,
            });
        }

        string input;
        ScanInputType inputType;

        if (!string.IsNullOrWhiteSpace(chip))
        {
            input = chip.Trim().ToUpperInvariant();
            inputType = ScanInputType.RfidChip;
        }
        else
        {
            input = qr!;
            inputType = ScanInputType.Qr;
        }

        var result = await sender.Send(
            new PerformClinicScanCommand(clinicId, input, inputType),
            cancellationToken);

        if (result.IsFailure)
            return BadRequest(new ProblemDetails { Detail = string.Join("; ", result.Errors), Status = 400 });

        if (!result.Value!.Matched)
            return NotFound(new ProblemDetails { Detail = "No registered pet matches this identifier.", Status = 404 });

        return Ok(result.Value);
    }
}
