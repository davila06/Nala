using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PawTrack.Application.Adoptions;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Adoptions;
using PawTrack.Domain.Pets;
using System.Security.Claims;

namespace PawTrack.API.Controllers;

[ApiController]
[Route("api/adoptions")]
public sealed class AdoptionsController(ISender sender, IBlobStorageService blobStorage) : ControllerBase
{
    // ── Public — anyone ───────────────────────────────────────────────────────

    [HttpGet("animals")]
    [EnableRateLimiting("public-api")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAnimals(
        [FromQuery] PetSpecies? species,
        [FromQuery] PetSize? size,
        [FromQuery] AgeCategory? ageCategory,
        [FromQuery] bool? isVaccinated,
        [FromQuery] bool? isSterilized,
        [FromQuery] bool? okWithKids,
        [FromQuery] bool? okWithDogs,
        [FromQuery] double? lat,
        [FromQuery] double? lng,
        [FromQuery] int? radiusKm,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        pageSize = Math.Clamp(pageSize, 1, 50);
        page = Math.Max(1, page);
        var result = await sender.Send(new GetAdoptablePetsQuery(
            species, size, ageCategory, isVaccinated, isSterilized,
            okWithKids, okWithDogs, lat, lng, radiusKm, page, pageSize),
            cancellationToken);
        return Ok(result.Value);
    }

    [HttpGet("animals/map")]
    [EnableRateLimiting("public-api")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAnimalsForMap(CancellationToken cancellationToken)
    {
        // Returns flat list (no pagination) — capped at 500 in the repository
        var result = await sender.Send(
            new GetAdoptablePetsQuery(null, null, null, null, null, null, null, null, null, null, 1, 500),
            cancellationToken);
        return Ok(result.Value?.Items);
    }

    [HttpGet("animals/{id:guid}")]
    [EnableRateLimiting("public-api")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAnimal(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetAdoptablePetByIdQuery(id), cancellationToken);
        if (result.IsFailure) return NotFound();
        return Ok(result.Value);
    }

    [HttpGet("fairs")]
    [EnableRateLimiting("public-api")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFairs(
        [FromQuery] double? lat,
        [FromQuery] double? lng,
        [FromQuery] int? radiusKm,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetUpcomingFairsQuery(lat, lng, radiusKm), cancellationToken);
        return Ok(result.Value);
    }

    // ── Owner — apply + view own applications ─────────────────────────────────

    [HttpPost("animals/{id:guid}/apply")]
    [Authorize(Roles = "Owner")]
    [EnableRateLimiting("public-api")]
    [RequestSizeLimit(4096)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Apply(
        Guid id,
        [FromBody] ApplyToAdoptRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await sender.Send(new ApplyToAdoptCommand(userId, id, request.Note), cancellationToken);
        if (result.IsFailure) return BadRequest(Problem(result));
        return Created($"/api/adoptions/applications/{result.Value!.Id}", result.Value);
    }

    [HttpDelete("applications/{applicationId:guid}")]
    [Authorize]
    [EnableRateLimiting("public-api")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Withdraw(Guid applicationId, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await sender.Send(new WithdrawApplicationCommand(userId, applicationId), cancellationToken);
        if (result.IsFailure) return BadRequest(Problem(result));
        return NoContent();
    }

    [HttpGet("applications/mine")]
    [Authorize]
    [EnableRateLimiting("public-api")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyApplications(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await sender.Send(
            new GetMyAdoptionApplicationsQuery(userId, Math.Max(1, page), Math.Clamp(pageSize, 1, 50)),
            cancellationToken);
        return Ok(result.Value);
    }

    // ── Ally Shelter — publish and manage ─────────────────────────────────────

    [HttpPost("animals")]
    [Authorize(Roles = "Ally")]
    [EnableRateLimiting("public-api")]
    [RequestSizeLimit(16_384)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Publish(
        [FromBody] PublishAdoptablePetRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await sender.Send(new PublishAdoptablePetCommand(
            userId, request.Name, request.Species, request.Size, request.AgeCategory,
            request.Story, request.RefLat, request.RefLng, request.RefLabel,
            request.Breed, request.AgeMonthsApprox, request.Requirements,
            request.MedicalNotes, request.IsVaccinated, request.IsSterilized,
            request.IsMicrochipped, request.OkWithKids, request.OkWithDogs,
            request.OkWithCats, request.NeedsYard), cancellationToken);
        if (result.IsFailure) return BadRequest(Problem(result));
        return Created($"/api/adoptions/animals/{result.Value!.Id}", result.Value);
    }

    [HttpPatch("animals/{id:guid}")]
    [Authorize(Roles = "Ally")]
    [EnableRateLimiting("public-api")]
    [RequestSizeLimit(8192)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateAdoptablePetRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await sender.Send(new UpdateAdoptablePetCommand(
            userId, id, request.Name, request.Story, request.Requirements,
            request.MedicalNotes, request.IsVaccinated, request.IsSterilized,
            request.IsMicrochipped, request.OkWithKids, request.OkWithDogs,
            request.OkWithCats, request.NeedsYard), cancellationToken);
        if (result.IsFailure) return BadRequest(Problem(result));
        return Ok(result.Value);
    }

    [HttpPost("animals/{id:guid}/photos")]
    [Authorize(Roles = "Ally")]
    [EnableRateLimiting("public-api")]
    [RequestSizeLimit(5_242_880)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UploadPhoto(
        Guid id,
        IFormFile photo,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        if (photo is null || photo.Length == 0)
            return BadRequest(new ProblemDetails { Title = "No file provided", Status = 400 });

        var result = await sender.Send(
            new UploadAdoptionPhotoCommand(userId, id, photo.OpenReadStream(), photo.ContentType, photo.FileName),
            cancellationToken);
        if (result.IsFailure) return BadRequest(Problem(result));
        return Ok(new { photoUrl = result.Value });
    }

    [HttpDelete("animals/{id:guid}/photos")]
    [Authorize(Roles = "Ally")]
    [EnableRateLimiting("public-api")]
    [RequestSizeLimit(512)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeletePhoto(
        Guid id,
        [FromBody] DeletePhotoRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await sender.Send(new DeleteAdoptionPhotoCommand(userId, id, request.PhotoUrl), cancellationToken);
        if (result.IsFailure) return BadRequest(Problem(result));
        return NoContent();
    }

    [HttpGet("animals/mine")]
    [Authorize(Roles = "Ally")]
    [EnableRateLimiting("public-api")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMine(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await sender.Send(
            new GetMyAdoptionAnimalsQuery(userId, Math.Max(1, page), Math.Clamp(pageSize, 1, 50)),
            cancellationToken);
        return Ok(result.Value);
    }

    [HttpGet("animals/{id:guid}/applications")]
    [Authorize(Roles = "Ally")]
    [EnableRateLimiting("public-api")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetApplications(
        Guid id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await sender.Send(
            new GetApplicationsForAnimalQuery(userId, id, Math.Max(1, page), Math.Clamp(pageSize, 1, 50)),
            cancellationToken);
        if (result.IsFailure) return Forbid();
        return Ok(result.Value);
    }

    [HttpPatch("applications/{applicationId:guid}/review")]
    [Authorize(Roles = "Ally")]
    [EnableRateLimiting("public-api")]
    [RequestSizeLimit(4096)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Review(
        Guid applicationId,
        [FromBody] ReviewApplicationRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await sender.Send(
            new ReviewAdoptionApplicationCommand(userId, applicationId, request.Approve, request.ReviewNote),
            cancellationToken);
        if (result.IsFailure) return BadRequest(Problem(result));
        return Ok(result.Value);
    }

    [HttpPatch("animals/{id:guid}/mark-adopted")]
    [Authorize(Roles = "Ally")]
    [EnableRateLimiting("public-api")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> MarkAdopted(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await sender.Send(new MarkAdoptedCommand(userId, id), cancellationToken);
        if (result.IsFailure) return BadRequest(Problem(result));
        return Ok(result.Value);
    }

    [HttpPost("fairs")]
    [Authorize(Roles = "Ally")]
    [EnableRateLimiting("public-api")]
    [RequestSizeLimit(8192)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateFair(
        [FromBody] CreateAdoptionFairRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await sender.Send(new CreateAdoptionFairCommand(
            userId, request.Title, request.VenueLabel, request.Lat, request.Lng,
            request.StartsAt, request.EndsAt, request.Description,
            request.AnimalIds ?? []), cancellationToken);
        if (result.IsFailure) return BadRequest(Problem(result));
        return Created($"/api/adoptions/fairs/{result.Value!.Id}", result.Value);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private bool TryGetUserId(out Guid userId)
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(claim, out userId);
    }

    private static ProblemDetails Problem<T>(Domain.Common.Result<T> result) => new()
    {
        Title = "Request failed",
        Detail = string.Join("; ", result.Errors),
        Status = 400,
    };
}

// ── Request body records ──────────────────────────────────────────────────────

public sealed record ApplyToAdoptRequest(string Note);
public sealed record ReviewApplicationRequest(bool Approve, string? ReviewNote);
public sealed record DeletePhotoRequest(string PhotoUrl);

public sealed record PublishAdoptablePetRequest(
    string Name,
    PetSpecies Species,
    PetSize Size,
    AgeCategory AgeCategory,
    string Story,
    double RefLat,
    double RefLng,
    string? RefLabel,
    string? Breed,
    int? AgeMonthsApprox,
    string? Requirements,
    string? MedicalNotes,
    bool IsVaccinated,
    bool IsSterilized,
    bool IsMicrochipped,
    bool OkWithKids,
    bool OkWithDogs,
    bool OkWithCats,
    bool NeedsYard);

public sealed record UpdateAdoptablePetRequest(
    string Name,
    string Story,
    string? Requirements,
    string? MedicalNotes,
    bool IsVaccinated,
    bool IsSterilized,
    bool IsMicrochipped,
    bool OkWithKids,
    bool OkWithDogs,
    bool OkWithCats,
    bool NeedsYard);

public sealed record CreateAdoptionFairRequest(
    string Title,
    string VenueLabel,
    double Lat,
    double Lng,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    string? Description,
    IReadOnlyList<Guid>? AnimalIds);
