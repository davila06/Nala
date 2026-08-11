using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PawTrack.API.Middleware;
using PawTrack.Application.Sightings.Commands.ReportFoundPet;
using PawTrack.Application.Sightings.Queries.GetActiveFoundPets;
using PawTrack.Application.Sightings.VisualMatch;
using PawTrack.Domain.Pets;

namespace PawTrack.API.Controllers;

[ApiController]
public sealed class FoundPetsController(ISender sender) : ControllerBase
{
    private static readonly HashSet<string> AllowedMimeTypes =
        ["image/jpeg", "image/png", "image/webp"];

    // ── POST /api/public/encontre/match — public AI match, no auth required ──
    [HttpPost("api/public/encontre/match")]
    [AllowAnonymous]
    [EnableRateLimiting("quick-match-public")]
    [RequestSizeLimit(5_242_880)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> QuickMatch(
        [FromForm] QuickMatchRequest request,
        CancellationToken ct)
    {
        if (request.Photo is null)
            return BadRequest(new ProblemDetails { Detail = "La foto es requerida.", Status = 400 });

        if (!AllowedMimeTypes.Contains(request.Photo.ContentType))
            return UnprocessableEntity(new ProblemDetails
            {
                Detail = "Solo se aceptan fotos JPEG, PNG o WebP.",
                Status = 422,
            });

        await using var stream = request.Photo.OpenReadStream();

        if (!ImageMagicBytesValidator.IsValidImage(stream, request.Photo.ContentType))
            return UnprocessableEntity(new ProblemDetails
            {
                Detail = "El contenido de la imagen no coincide con el tipo de archivo declarado.",
                Status = 422,
            });

        // Copy to a MemoryStream so the handler can seek and re-read if needed
        var ms = new MemoryStream();
        await stream.CopyToAsync(ms, ct);
        ms.Position = 0;

        var result = await sender.Send(new PublicQuickMatchQuery(
            ms, request.Photo.ContentType,
            request.Lat, request.Lng), ct);

        if (result.IsFailure)
            return UnprocessableEntity(new ProblemDetails { Detail = result.Errors.First(), Status = 422 });

        return Ok(result.Value);
    }

    // ── POST /api/found-pets — anonymous, rate-limited ───────────────────────
    [HttpPost("api/found-pets")]
    [AllowAnonymous]
    [EnableRateLimiting("sightings")]
    [RequestSizeLimit(5_242_880)] // 5 MB
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> ReportFoundPet(
        [FromForm] ReportFoundPetRequest request,
        CancellationToken cancellationToken)
    {
        Stream? photoStream = null;
        string? contentType = null;

        if (request.Photo is not null)
        {
            contentType = request.Photo.ContentType;
            if (!AllowedMimeTypes.Contains(contentType))
            {
                return UnprocessableEntity(new ProblemDetails
                {
                    Title = "Validation error",
                    Status = 422,
                    Extensions = { ["errors"] = new[] { "Photo must be JPEG, PNG, or WebP." } },
                });
            }
            photoStream = request.Photo.OpenReadStream();

            // Verify file signature (magic bytes) — rejects executables/scripts
            // disguised with a spoofed Content-Type header.
            if (!ImageMagicBytesValidator.IsValidImage(photoStream, contentType))
            {
                await photoStream.DisposeAsync();
                return UnprocessableEntity(new ProblemDetails
                {
                    Title = "Validation error",
                    Status = 422,
                    Extensions = { ["errors"] = new[] { "Photo content does not match the declared file type." } },
                });
            }
        }

        var command = new ReportFoundPetCommand(
            request.FoundSpecies,
            request.BreedEstimate,
            request.ColorDescription,
            request.SizeEstimate,
            request.FoundLat,
            request.FoundLng,
            request.ContactName,
            request.ContactPhone,
            request.Note,
            photoStream,
            contentType);

        var result = await sender.Send(command, cancellationToken);

        if (photoStream is not null)
            await photoStream.DisposeAsync();

        if (result.IsFailure)
        {
            return UnprocessableEntity(new ProblemDetails
            {
                Title = "Validation error",
                Status = 422,
                Extensions = { ["errors"] = result.Errors },
            });
        }

        return Created(
            $"/api/found-pets/{result.Value!.ReportId}",
            new
            {
                reportId = result.Value.ReportId,
                candidates = result.Value.Candidates,
            });
    }

    // ── GET /api/found-pets/active — public, returns open reports ────────────
    [HttpGet("api/found-pets/active")]
    [AllowAnonymous]
    [EnableRateLimiting("public-api")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetActiveFoundPets(
        [FromQuery] int maxResults = 50,
        CancellationToken cancellationToken = default)
    {
        var query = new GetActiveFoundPetsQuery(maxResults);
        var result = await sender.Send(query, cancellationToken);

        return result.IsFailure
            ? StatusCode(500, new ProblemDetails { Title = "Internal error", Status = 500 })
            : Ok(result.Value);
    }
}

// ── Request model ────────────────────────────────────────────────────────────

public sealed class ReportFoundPetRequest
{
    public PetSpecies FoundSpecies { get; set; }
    public string? BreedEstimate { get; set; }
    public string? ColorDescription { get; set; }
    public string? SizeEstimate { get; set; }
    public double FoundLat { get; set; }
    public double FoundLng { get; set; }
    public string ContactName { get; set; } = string.Empty;
    public string ContactPhone { get; set; } = string.Empty;
    public string? Note { get; set; }
    public IFormFile? Photo { get; set; }
}

public sealed class QuickMatchRequest
{
    public IFormFile? Photo { get; set; }
    public double? Lat { get; set; }
    public double? Lng { get; set; }
}
