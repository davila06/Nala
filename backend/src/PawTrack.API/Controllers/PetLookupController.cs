using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PawTrack.Application.Clinics.Commands.PerformClinicScan;
using PawTrack.Application.Clinics.Queries.GetMyClinic;
using PawTrack.Domain.Clinics;
using System.Security.Claims;

namespace PawTrack.API.Controllers;

/// <summary>
/// Machine-to-machine pet lookup API for Clínica Partner tier.
/// Authenticated via X-PawTrack-Key header (ClinicApiKeyMiddleware).
/// </summary>
[ApiController]
[Route("api/v1/pets")]
[Authorize(Roles = "Clinic")]
public sealed class PetLookupController(ISender sender) : ControllerBase
{
    [HttpGet("lookup")]
    [EnableRateLimiting("public-api")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Lookup(
        [FromQuery] string? chip,
        [FromQuery] string? qr,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(chip) && string.IsNullOrWhiteSpace(qr))
            return BadRequest(new ProblemDetails { Detail = "Provide 'chip' or 'qr' query parameter.", Status = 400 });

        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var clinicResult = await sender.Send(new GetMyClinicQuery(userId), cancellationToken);
        if (clinicResult.IsFailure || clinicResult.Value is null) return Forbid();

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
            new PerformClinicScanCommand(clinicResult.Value.Id, input, inputType),
            cancellationToken);

        if (result.IsFailure)
            return BadRequest(new ProblemDetails { Detail = string.Join("; ", result.Errors), Status = 400 });

        if (!result.Value!.Matched)
            return NotFound(new ProblemDetails { Detail = "No registered pet matches this identifier.", Status = 404 });

        return Ok(result.Value);
    }
}
