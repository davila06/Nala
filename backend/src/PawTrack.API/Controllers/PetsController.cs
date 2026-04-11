using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Pets.Commands.CreatePet;
using PawTrack.Application.Pets.Commands.DeletePet;
using PawTrack.Application.Pets.Commands.UpdatePet;
using PawTrack.Application.Pets.Queries.GetPetScanHistory;
using PawTrack.Application.Pets.Queries.GetMyPets;
using PawTrack.Application.Pets.Queries.GetPetDetail;
using PawTrack.Application.Pets.Queries.GetPublicPetProfile;
using PawTrack.Domain.Pets;
using System.Security.Claims;
using Microsoft.AspNetCore.RateLimiting;

namespace PawTrack.API.Controllers;

[ApiController]
[Route("api/pets")]
[Authorize]
public sealed class PetsController(
    ISender sender,
    IQrCodeService qrCodeService,
    IWhatsAppAvatarService whatsAppAvatarService,
    IPublicAppUrlProvider publicAppUrlProvider,
    IAvatarTokenService avatarTokenService) : ControllerBase
{
    // ── GET /api/pets ─────────────────────────────────────────────────────────
    [HttpGet]
    [EnableRateLimiting("public-api")] // 30/min — each call issues GetMyPetsQuery (DB SELECT)
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyPets(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        var result = await sender.Send(new GetMyPetsQuery(userId), cancellationToken);
        return Ok(result.Value);
    }

    // ── GET /api/pets/{id} ────────────────────────────────────────────────────
    [HttpGet("{id:guid}")]
    [EnableRateLimiting("public-api")] // 30/min — each call issues GetPetDetailQuery (DB SELECT)
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetPetDetail(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        var result = await sender.Send(new GetPetDetailQuery(id, userId), cancellationToken);

        if (result.IsFailure)
        {
            return result.Errors.Contains("Access denied.")
                ? Forbid()
                : NotFound(new ProblemDetails { Title = "Pet not found", Status = 404 });
        }

        return Ok(result.Value);
    }

    // ── POST /api/pets ────────────────────────────────────────────────────────
    [HttpPost]    [EnableRateLimiting("public-api")] // 30/min — each call can upload a 5 MB photo to Azure Blob Storage    [Consumes("multipart/form-data")]
    [RequestSizeLimit(5_242_880)] // 5 MB
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> CreatePet(
        [FromForm] CreatePetRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        var (photoBytes, contentType, fileName) = await ReadPhotoAsync(request.Photo);

        var command = new CreatePetCommand(
            userId,
            request.Name,
            request.Species,
            request.Breed,
            request.BirthDate,
            photoBytes,
            contentType,
            fileName);

        var result = await sender.Send(command, cancellationToken);

        if (result.IsFailure)
            return BadRequest(new ProblemDetails
            {
                Title = "Create pet failed",
                Detail = string.Join("; ", result.Errors),
                Status = 400,
            });

        return Created($"/api/pets/{result.Value}", new { petId = result.Value });
    }

    // ── PUT /api/pets/{id} ────────────────────────────────────────────────────
    [HttpPut("{id:guid}")]    [EnableRateLimiting("public-api")] // 30/min — each call overwrites the Blob photo (up to 5 MB)    [Consumes("multipart/form-data")]
    [RequestSizeLimit(5_242_880)] // 5 MB
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdatePet(
        Guid id,
        [FromForm] UpdatePetRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        var (photoBytes, contentType, fileName) = await ReadPhotoAsync(request.Photo);

        var command = new UpdatePetCommand(
            id,
            userId,
            request.Name,
            request.Species,
            request.Breed,
            request.BirthDate,
            photoBytes,
            contentType,
            fileName);

        var result = await sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return result.Errors.Contains("Access denied.")
                ? Forbid()
                : NotFound(new ProblemDetails { Title = "Pet not found", Status = 404 });
        }

        return Ok(new { petId = result.Value!.Value.ToString() });
    }

    // ── DELETE /api/pets/{id} ─────────────────────────────────────────────────
    [HttpDelete("{id:guid}")]    [EnableRateLimiting("public-api")] // 30/min — each call invokes Blob delete + DB write    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeletePet(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        var result = await sender.Send(new DeletePetCommand(id, userId), cancellationToken);

        if (result.IsFailure)
        {
            return result.Errors.Contains("Access denied.")
                ? Forbid()
                : NotFound(new ProblemDetails { Title = "Pet not found", Status = 404 });
        }

        return NoContent();
    }

    // ── GET /api/pets/{id}/qr ─────────────────────────────────────────────────
    [HttpGet("{id:guid}/qr")]
    [EnableRateLimiting("public-api")] // 30/min — CPU-intensive: IQrCodeService.GeneratePng() + DB read
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetQrCode(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        // Verify ownership before generating QR
        var result = await sender.Send(new GetPetDetailQuery(id, userId), cancellationToken);

        if (result.IsFailure)
        {
            return result.Errors.Contains("Access denied.")
                ? Forbid()
                : NotFound(new ProblemDetails { Title = "Pet not found", Status = 404 });
        }

        // QR encodes the public profile URL — frontend route /p/{petId}
        var url = $"https://pawtrack.cr/p/{id}";
        var pngBytes = qrCodeService.GeneratePng(url);

        return File(pngBytes, "image/png", $"qr-{id}.png");
    }

    // ── GET /api/pets/{id}/whatsapp-avatar ───────────────────────────────────
    [AllowAnonymous]
    [HttpGet("{id:guid}/whatsapp-avatar")]
        [EnableRateLimiting("whatsapp-avatar")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetWhatsAppAvatar(
        Guid id,
        [FromQuery] string? token,
        CancellationToken cancellationToken)
    {
        // If a token is provided, validate it (ephemeral access); otherwise fall through to public access
        if (token is not null && !avatarTokenService.Validate(id, token))
            return Unauthorized(new ProblemDetails { Title = "Invalid or expired avatar token.", Status = 401 });

        var petResult = await sender.Send(new GetPublicPetProfileQuery(id), cancellationToken);
        if (petResult.IsFailure || petResult.Value is null)
            return NotFound(new ProblemDetails { Title = "Pet not found", Status = 404 });

        var pet = petResult.Value;
        var baseUrl = publicAppUrlProvider.GetBaseUrl();
        var profileUrl = $"{baseUrl}/p/{id}";

        var avatarBytes = await whatsAppAvatarService.BuildAvatarAsync(
            pet.PhotoUrl,
            profileUrl,
            pet.Name,
            cancellationToken);

        // Short cache — tokens are ephemeral; public access cached 5 min
        Response.Headers.CacheControl = token is null ? "public,max-age=300" : "private,max-age=0,no-store";
        return File(avatarBytes, "image/png", $"whatsapp-avatar-{id}.png");
    }

    // ── POST /api/pets/{id}/avatar-token ──────────────────────────────────────
    /// <summary>
    /// Generates a short-lived (60 min) HMAC-signed token allowing anonymous access
    /// to the WhatsApp avatar for the specified pet. Only the pet owner may request it.
    /// </summary>
    [HttpPost("{id:guid}/avatar-token")]
    [EnableRateLimiting("public-api")] // 30/min — each call signs an HMAC token + issues DB SELECT
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GenerateAvatarToken(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        // Verify ownership
        var petResult = await sender.Send(new GetPetDetailQuery(id, userId), cancellationToken);
        if (petResult.IsFailure)
        {
            return petResult.Errors.Contains("Access denied.")
                ? Forbid()
                : NotFound(new ProblemDetails { Title = "Pet not found", Status = 404 });
        }

        var avatarToken = avatarTokenService.Generate(id);
        return Ok(new { Token = avatarToken });
    }

    // ── GET /api/pets/{id}/scan-history ──────────────────────────────────────
    [HttpGet("{id:guid}/scan-history")]
    [EnableRateLimiting("public-api")] // 30/min — each call issues GetPetScanHistoryQuery (unbounded DB SELECT)
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetScanHistory(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        var result = await sender.Send(new GetPetScanHistoryQuery(id, userId), cancellationToken);

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

// ── Request models ─────────────────────────────────────────────────────────────
public sealed record CreatePetRequest(
    string Name,
    PetSpecies Species,
    string? Breed,
    DateOnly? BirthDate,
    IFormFile? Photo);

public sealed record UpdatePetRequest(
    string Name,
    PetSpecies Species,
    string? Breed,
    DateOnly? BirthDate,
    IFormFile? Photo);
