using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PawTrack.Application.LostPets.Commands.ReportLostPet;
using PawTrack.Application.LostPets.Commands.UpdateLostPetStatus;
using PawTrack.Application.LostPets.Queries.GetActiveLostPetByPet;
using PawTrack.Application.LostPets.Queries.GetCaseRoom;
using PawTrack.Application.LostPets.Queries.GetLostPetContact;
using PawTrack.Application.LostPets.Queries.GetLostPetEventById;
using PawTrack.Domain.LostPets;
using System.Security.Claims;

namespace PawTrack.API.Controllers;

[ApiController]
[Route("api/lost-pets")]
[Authorize]
public sealed class LostPetsController(ISender sender) : ControllerBase
{
    // ── POST /api/lost-pets ───────────────────────────────────────────────────
    [HttpPost]    [EnableRateLimiting("public-api")] // 30/min — each call writes DB, may upload Blob Storage, dispatches notifications    [Consumes("multipart/form-data")]
    [RequestSizeLimit(5_242_880)] // 5 MB — same ceiling as PetsController and SightingsController
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ReportLostPet(
        [FromForm] ReportLostPetRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        var (photoBytes, contentType, fileName) = await ReadPhotoAsync(request.RecentPhoto);

        var result = await sender.Send(new ReportLostPetCommand(
            request.PetId,
            userId,
            request.Description,
            request.PublicMessage,
            request.LastSeenLat,
            request.LastSeenLng,
            request.LastSeenAt ?? DateTimeOffset.UtcNow,
            photoBytes,
            contentType,
            fileName,
            request.ContactName,
            request.ContactPhone,
            request.RewardAmount,
            request.RewardNote), cancellationToken);

        if (result.IsFailure)
        {
            if (result.Errors.Contains("Access denied.")) return Forbid();
            if (result.Errors.Contains("Pet not found.")) return NotFound(new ProblemDetails { Title = "Pet not found", Status = 404 });
            return UnprocessableEntity(new ProblemDetails
            {
                Title = "Validation error",
                Status = 422,
                Extensions = { ["errors"] = result.Errors },
            });
        }

        return CreatedAtAction(nameof(GetById), new { id = result.Value }, new { id = result.Value });
    }

    // ── GET /api/lost-pets/{id} ───────────────────────────────────────────────
    [HttpGet("{id:guid}")]    [EnableRateLimiting("public-api")] // 30/min — event IDs are enumerable from public map; throttle prevents bulk harvest    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        var result = await sender.Send(new GetLostPetEventByIdQuery(id, userId), cancellationToken);

        if (result.IsFailure)
        {
            return result.Errors.Contains("Access denied.")
                ? Forbid()
                : NotFound(new ProblemDetails { Title = "Lost pet report not found", Status = 404 });
        }

        return Ok(result.Value);
    }

    // ── GET /api/lost-pets/by-pet/{petId} ────────────────────────────────────
    [HttpGet("by-pet/{petId:guid}")]    [EnableRateLimiting("public-api")] // 30/min — consistent throttle across all LostPets reads    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetActiveByPet(Guid petId, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        var result = await sender.Send(new GetActiveLostPetByPetQuery(petId, userId), cancellationToken);

        if (result.IsFailure)
        {
            return result.Errors.Contains("Access denied.")
                ? Forbid()
                : NotFound(new ProblemDetails { Title = "Pet not found", Status = 404 });
        }

        if (result.Value is null)
            return Ok(null);

        return Ok(result.Value);
    }

    // ── GET /api/lost-pets/{id}/contact ─────────────────────────────────────
    /// <summary>
    /// Returns the emergency contact details (name + phone) for an active lost-pet report.
    /// Requires authentication. ContactPhone is never exposed on public endpoints.
    /// </summary>
    [HttpGet("{id:guid}/contact")]    [EnableRateLimiting("contact-lookup")] // 10/min — each response discloses a real phone number    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetContact(Guid id, CancellationToken cancellationToken)
    {
        // TryGetUserId validates the JWT claim; Unauthorized is returned if missing.
        if (!TryGetUserId(out _))
            return Unauthorized();

        var result = await sender.Send(new GetLostPetContactQuery(id), cancellationToken);

        if (result.IsFailure)
            return NotFound(new ProblemDetails { Title = "Contact not found", Status = 404 });

        return Ok(result.Value);
    }
    // ── GET /api/lost-pets/{id}/case ───────────────────────────────────────────────
    /// <summary>
    /// Returns the Case Room aggregate for <paramref name="id"/>: the full lost-pet
    /// event, all linked sightings ranked by priority score, and the anonymised
    /// log of nearby-alert notifications dispatched for this case.
    /// Only the event owner can access this endpoint.
    /// </summary>
    [HttpGet("{id:guid}/case")]
    [EnableRateLimiting("public-api")] // 30/min — most DB-intensive query (event + sightings + notification log)
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCaseRoom(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        var result = await sender.Send(new GetCaseRoomQuery(id, userId), cancellationToken);

        if (result.IsFailure)
        {
            return result.Errors.Contains("Access denied.")
                ? Forbid()
                : NotFound(new ProblemDetails { Title = "Lost pet report not found", Status = 404 });
        }

        return Ok(result.Value);
    }
    // ── PUT /api/lost-pets/{id}/status ────────────────────────────────────────
    [HttpPut("{id:guid}/status")]
    [EnableRateLimiting("public-api")] // 30/min — triggers state machine + may dispatch domain events
    [RequestSizeLimit(512)] // status enum string + optional GUID — max ~80 B; 512 B ceiling
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> UpdateStatus(
        Guid id,
        [FromBody] UpdateLostPetStatusRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        var command = new UpdateLostPetStatusCommand(
            id,
            userId,
            request.NewStatus,
            request.ConfirmedSightingId);

        var result = await sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            if (result.Errors.Contains("Access denied.")) return Forbid();
            if (result.Errors.Contains("Lost pet report not found."))
                return NotFound(new ProblemDetails { Title = "Lost pet report not found", Status = 404 });
            return UnprocessableEntity(new ProblemDetails
            {
                Title = "Invalid status transition",
                Status = 422,
                Extensions = { ["errors"] = result.Errors },
            });
        }

        return NoContent();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    private bool TryGetUserId(out Guid userId)
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? User.FindFirstValue("sub");
        return Guid.TryParse(claim, out userId);
    }

    private static async Task<(byte[]? Bytes, string? ContentType, string? FileName)> ReadPhotoAsync(
        IFormFile? photo)
    {
        if (photo is null || photo.Length == 0)
            return (null, null, null);

        using var ms = new MemoryStream();
        await photo.CopyToAsync(ms);
        return (ms.ToArray(), photo.ContentType, photo.FileName);
    }
}

// ── Request models ──────────────────────────────────────────────────────────────
public sealed record ReportLostPetRequest(
    Guid PetId,
    string? Description,
    string? PublicMessage,
    double? LastSeenLat,
    double? LastSeenLng,
    DateTimeOffset? LastSeenAt,
    IFormFile? RecentPhoto,
    string? ContactName,
    string? ContactPhone,
    decimal? RewardAmount,
    string? RewardNote);

public sealed record UpdateLostPetStatusRequest(
    LostPetStatus NewStatus,
    Guid? ConfirmedSightingId = null);
