using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PawTrack.Application.Certificates.Commands.IssueCertificate;
using PawTrack.Application.Certificates.Queries.GetCertificatesForClinic;
using PawTrack.Application.Certificates.Queries.GetCertificatesForPet;
using PawTrack.Application.Certificates.Queries.VerifyCertificate;
using PawTrack.Domain.Certificates;
using System.Security.Claims;

namespace PawTrack.API.Controllers;

[ApiController]
[Route("api/certificates")]
[Authorize]
public sealed class CertificatesController(ISender sender) : ControllerBase
{
    // ── GET /api/certificates/clinic/{clinicId}?page=1 ──────────────────────
    [HttpGet("clinic/{clinicId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetForClinic(
        Guid clinicId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(
            new GetCertificatesForClinicQuery(clinicId, page, pageSize),
            cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Errors);
    }

    // ── GET /api/certificates/pet/{petId} ─────────────────────────────────────
    [HttpGet("pet/{petId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetForPet(Guid petId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetCertificatesForPetQuery(petId), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Errors);
    }

    // ── GET /api/certificates/verify/{code} ───────────────────────────────────
    [HttpGet("verify/{code}")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Verify(string code, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new VerifyCertificateQuery(code.ToUpperInvariant()), cancellationToken);
        if (result.IsFailure || result.Value is null) return NotFound();
        return Ok(result.Value);
    }

    // ── POST /api/certificates ────────────────────────────────────────────────
    [HttpPost]
    [EnableRateLimiting("public-api")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Issue(
        [FromBody] IssueCertificateRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var result = await sender.Send(new IssueCertificateCommand(
            request.PetId,
            request.ClinicId,
            userId,
            request.Type,
            request.Notes,
            request.ValidUntil,
            request.PetName,
            request.PetSpecies,
            request.PetBreed,
            request.ClinicName,
            request.ClinicLicense,
            request.VetName), cancellationToken);

        if (result.IsFailure)
            return UnprocessableEntity(new ProblemDetails { Detail = string.Join(", ", result.Errors) });

        return Created($"api/certificates/verify/{result.Value!.VerificationCode}", result.Value);
    }

    private bool TryGetUserId(out Guid userId)
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(raw, out userId);
    }
}

public sealed record IssueCertificateRequest(
    Guid PetId,
    Guid ClinicId,
    CertificateType Type,
    string? Notes,
    DateTimeOffset? ValidUntil,
    string PetName,
    string PetSpecies,
    string? PetBreed,
    string ClinicName,
    string ClinicLicense,
    string VetName);
