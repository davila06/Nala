using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PawTrack.Application.Certificates.Commands;
using PawTrack.Application.Certificates.Commands.IssueCertificate;
using PawTrack.Application.Certificates.Commands.RevokeCertificate;
using PawTrack.Application.Certificates.Interfaces;
using PawTrack.Application.Certificates.Queries.DownloadCertificatePdf;
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
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var isAdmin = User.IsInRole("Admin");
        var result = await sender.Send(
            new GetCertificatesForClinicQuery(clinicId, userId, isAdmin, page, pageSize),
            cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Errors);
    }

    // ── GET /api/certificates/pet/{petId} ─────────────────────────────────────
    [HttpGet("pet/{petId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetForPet(Guid petId, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var isAdmin = User.IsInRole("Admin");
        var result = await sender.Send(new GetCertificatesForPetQuery(petId, userId, isAdmin), cancellationToken);
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

    // ── POST /api/certificates/passport ───────────────────────────────────────
    // Emits an OIRSA-format vaccine passport (Clinic Partner only)
    [HttpPost("passport")]
    [Authorize(Roles = "Clinic")]
    [EnableRateLimiting("public-api")]
    [RequestSizeLimit(2048)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> IssuePassport(
        [FromBody] IssueVaccinePassportRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var vaccines = request.Vaccines
            .Select(v => new PassportVaccineEntryInput(v.VaccineName, v.Brand, v.LotNumber, v.ApplicationDate, v.ValidUntil))
            .ToList().AsReadOnly() as System.Collections.Generic.IReadOnlyList<PassportVaccineEntryInput>;
        var parasite = request.ParasiteControl is { } pc
            ? new PassportParasiteEntryInput(pc.ProductName, pc.ApplicationDate, pc.NextDueDate)
            : null;
        var result = await sender.Send(
            new IssueVaccinePassportCommand(
                request.PetId, request.ClinicId, userId,
                request.VeterinarianId,
                request.VetName, request.VetLicense, request.PetColor,
                vaccines!, parasite),
            cancellationToken);
        if (result.IsFailure)
            return UnprocessableEntity(new ProblemDetails { Detail = string.Join(", ", result.Errors) });
        return Created($"api/certificates/verify/{result.Value!.VerificationCode}", result.Value);
    }

    // ── GET /api/certificates/{id}/download ──────────────────────────────────
    [HttpGet("{id:guid}/download")]
    [EnableRateLimiting("public-api")]
    [Produces("application/pdf")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Download(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var result = await sender.Send(
            new DownloadCertificatePdfQuery(id, userId, User.IsInRole("Admin")),
            cancellationToken);

        if (result.IsFailure)
            return UnprocessableEntity(new ProblemDetails { Detail = string.Join(", ", result.Errors), Status = 422 });

        return File(result.Value!.Bytes, result.Value.ContentType, result.Value.FileName);
    }

    // ── POST /api/certificates/{id}/revoke ───────────────────────────────────
    [HttpPost("{id:guid}/revoke")]
    [EnableRateLimiting("public-api")]
    [RequestSizeLimit(512)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Revoke(
        Guid id,
        [FromBody] RevokeCertificateRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var isAdmin = User.IsInRole("Admin");
        var result = await sender.Send(
            new RevokeCertificateCommand(id, userId, isAdmin, request.Reason),
            cancellationToken);

        if (result.IsFailure)
            return UnprocessableEntity(new ProblemDetails { Detail = string.Join(", ", result.Errors) });

        return Ok(result.Value);
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

public sealed record IssueVaccinePassportRequest(
    Guid PetId,
    Guid ClinicId,
    Guid VeterinarianId,
    string VetName,
    string? VetLicense,
    string? PetColor,
    IReadOnlyList<VaccineEntryRequest> Vaccines,
    ParasiteControlRequest? ParasiteControl);

public sealed record VaccineEntryRequest(
    string VaccineName,
    string? Brand,
    string? LotNumber,
    DateOnly ApplicationDate,
    DateOnly? ValidUntil);

public sealed record ParasiteControlRequest(
    string ProductName,
    DateOnly ApplicationDate,
    DateOnly? NextDueDate);

public sealed record RevokeCertificateRequest(string Reason);
