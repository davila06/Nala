using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PawTrack.API.Middleware;
using PawTrack.Application.Sightings.Commands.ReportSighting;
using PawTrack.Application.Sightings.Queries.GetSightingsByPet;
using PawTrack.Application.Sightings.VisualMatch;
using System.Security.Claims;

namespace PawTrack.API.Controllers;

[ApiController]
[Route("api/sightings")]
public sealed class SightingsController(ISender sender) : ControllerBase
{
    private static readonly HashSet<string> AllowedMimeTypes =
        ["image/jpeg", "image/png", "image/webp"];

    // ── POST /api/sightings — anonymous, rate-limited ─────────────────────────
    [HttpPost]
    [AllowAnonymous]
    [EnableRateLimiting("sightings")]
    [RequestSizeLimit(5_242_880)] // 5 MB
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> ReportSighting(
        [FromForm] ReportSightingRequest request,
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

        var command = new ReportSightingCommand(
            request.PetId,
            request.Lat,
            request.Lng,
            request.Note,
            photoStream,
            contentType,
            request.SightedAt ?? DateTimeOffset.UtcNow);

        var result = await sender.Send(command, cancellationToken);

        if (photoStream is not null)
            await photoStream.DisposeAsync();

        if (result.IsFailure)
        {
            if (result.Errors.Contains("Pet not found."))
                return NotFound(new ProblemDetails { Title = "Pet not found", Status = 404 });

            return UnprocessableEntity(new ProblemDetails
            {
                Title = "Validation error",
                Status = 422,
                Extensions = { ["errors"] = result.Errors },
            });
        }

        return Created($"/api/sightings/{result.Value}", new { id = result.Value });
    }

    // ── POST /api/sightings/visual-match — anonymous, rate-limited ───────────
    /// <summary>
    /// Accepts a probe photo and optional GPS coordinates.
    /// Returns the up-to-5 active lost-pet profiles whose photo best resembles the uploaded photo.
    /// Does not create a sighting report; purely a read/match operation.
    /// </summary>
    [HttpPost("visual-match")]
    [AllowAnonymous]
    [EnableRateLimiting("sightings")]
    [RequestSizeLimit(5_242_880)] // 5 MB — matches the in-handler check and POST /api/sightings behaviour
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> VisualMatch(
        [FromForm] VisualMatchRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Photo is null)
            return UnprocessableEntity(new ProblemDetails
            {
                Title  = "Validation error",
                Status = 422,
                Extensions = { ["errors"] = new[] { "Photo is required." } },
            });

        const long maxBytes = 5 * 1024 * 1024;
        if (request.Photo.Length > maxBytes)
            return UnprocessableEntity(new ProblemDetails
            {
                Title  = "Validation error",
                Status = 422,
                Extensions = { ["errors"] = new[] { "Photo must be ≤ 5 MB." } },
            });

        if (!AllowedMimeTypes.Contains(request.Photo.ContentType))
            return UnprocessableEntity(new ProblemDetails
            {
                Title  = "Validation error",
                Status = 422,
                Extensions = { ["errors"] = new[] { "Photo must be JPEG, PNG, or WebP." } },
            });

        await using var stream = request.Photo.OpenReadStream();

        // Verify file signature (magic bytes) — rejects disguised malicious files.
        if (!ImageMagicBytesValidator.IsValidImage(stream, request.Photo.ContentType))
            return UnprocessableEntity(new ProblemDetails
            {
                Title  = "Validation error",
                Status = 422,
                Extensions = { ["errors"] = new[] { "Photo content does not match the declared file type." } },
            });

        var result = await sender.Send(
            new MatchSightingPhotoQuery(
                stream,
                request.Photo.ContentType,
                request.Lat,
                request.Lng),
            cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : UnprocessableEntity(new ProblemDetails
            {
                Title  = "Processing error",
                Status = 422,
                Extensions = { ["errors"] = result.Errors },
            });
    }

    // ── POST /api/sightings/{sightingId}/visual-match — anonymous, rate-limited ─
    /// <summary>
    /// Finds the best matching active lost-pet profiles for a sighting whose photo
    /// is already stored in Blob Storage.  Useful for auto-match shown right after
    /// the reporter submits the sighting form.
    /// </summary>
    [HttpPost("{sightingId:guid}/visual-match")]
    [AllowAnonymous]
    [EnableRateLimiting("sightings")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> VisualMatchBySighting(
        Guid sightingId,
        [FromQuery] double? lat,
        [FromQuery] double? lng,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new MatchSightingByIdQuery(sightingId, lat, lng),
            cancellationToken);

        if (result.IsFailure)
        {
            return result.Errors.Contains("Sighting not found.")
                || result.Errors.Contains("Sighting has no photo.")
                ? NotFound(new ProblemDetails { Title = result.Errors[0], Status = 404 })
                : UnprocessableEntity(new ProblemDetails
                {
                    Title  = "Processing error",
                    Status = 422,
                    Extensions = { ["errors"] = result.Errors },
                });
        }

        return Ok(result.Value);
    }

    // ── GET /api/sightings/pet/{petId} — owner only ───────────────────────────
    [HttpGet("pet/{petId:guid}")]
    [Authorize]    [EnableRateLimiting("public-api")]    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSightingsByPet(
        Guid petId,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        var result = await sender.Send(
            new GetSightingsByPetQuery(petId, userId), cancellationToken);

        if (result.IsFailure)
        {
            return result.Errors.Contains("Access denied.")
                ? Forbid()
                : NotFound(new ProblemDetails { Title = "Pet not found", Status = 404 });
        }

        return Ok(result.Value);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    private bool TryGetUserId(out Guid userId)
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? User.FindFirstValue("sub");
        return Guid.TryParse(claim, out userId);
    }
}

// ── Request models ────────────────────────────────────────────────────────────
public sealed record VisualMatchRequest(
    IFormFile? Photo,
    [property: FromForm(Name = "lat")] double? Lat,
    [property: FromForm(Name = "lng")] double? Lng);

public sealed record ReportSightingRequest(
    [property: FromForm(Name = "petId")]    Guid PetId,
    [property: FromForm(Name = "lat")]      double Lat,
    [property: FromForm(Name = "lng")]      double Lng,
    [property: FromForm(Name = "note")]     string? Note,
    [property: FromForm(Name = "sightedAt")]DateTimeOffset? SightedAt,
    IFormFile? Photo);
