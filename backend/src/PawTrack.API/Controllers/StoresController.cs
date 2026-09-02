using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PawTrack.Application.Stores;
using PawTrack.Domain.Stores;
using System.Security.Claims;

namespace PawTrack.API.Controllers;

[ApiController]
[Route("api/stores")]
public sealed class StoresController(ISender sender) : ControllerBase
{
    // ── POST /api/stores/register — public registration ──────────────────────
    [HttpPost("register")]
    [AllowAnonymous]
    [EnableRateLimiting("register")]
    [RequestSizeLimit(2048)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Register(
        [FromBody] RegisterStoreRequest request,
        CancellationToken ct)
    {
        var result = await sender.Send(new RegisterStoreCommand(
            request.Name, request.Description, request.Address,
            request.Lat, request.Lng, request.ContactEmail, request.Password), ct);

        if (result.IsFailure)
        {
            if (result.Errors.Contains("duplicate_email"))
                return Created(string.Empty, new { message = "Solicitud recibida. Si es elegible, recibirá confirmación." });
            return UnprocessableEntity(new ProblemDetails { Detail = string.Join("; ", result.Errors), Status = 422 });
        }

        return Created(string.Empty, result.Value);
    }

    // ── GET /api/stores/mine — store owner profile ────────────────────────────
    [HttpGet("mine")]
    [Authorize(Roles = "Store")]
    [EnableRateLimiting("public-api")]
    public async Task<IActionResult> GetMine(CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await sender.Send(new GetMyStoreQuery(userId), ct);
        return result.IsSuccess ? Ok(result.Value) : NotFound(result.Errors);
    }

    // ── PUT /api/stores/profile ───────────────────────────────────────────────
    [HttpPut("profile")]
    [Authorize(Roles = "Store")]
    [EnableRateLimiting("public-api")]
    [RequestSizeLimit(2048)]
    public async Task<IActionResult> UpdateProfile(
        [FromBody] UpdateStoreProfileRequest request,
        CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await sender.Send(new UpdateStoreProfileCommand(
            userId, request.Name, request.Description, request.Address,
            request.Lat, request.Lng, request.PhoneNumber, request.Website), ct);
        if (result.IsFailure)
            return UnprocessableEntity(new ProblemDetails { Detail = string.Join("; ", result.Errors), Status = 422 });
        return Ok(result.Value);
    }

    // ── GET /api/stores/me/analytics?year=&month=&locationId= — StorePlus/Partner gate ─
    [HttpGet("me/analytics")]
    [Authorize(Roles = "Store")]
    [EnableRateLimiting("public-api")]
    public async Task<IActionResult> GetAnalytics(
        [FromQuery] int? year,
        [FromQuery] int? month,
        [FromQuery] Guid? locationId,
        CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var now = DateTimeOffset.UtcNow;
        var result = await sender.Send(
            new GetStoreAnalyticsQuery(userId, year ?? now.Year, month ?? now.Month, locationId), ct);
        if (result.IsFailure)
            return UnprocessableEntity(new ProblemDetails { Detail = string.Join("; ", result.Errors), Status = 422 });
        return Ok(result.Value);
    }

    // ── GET /api/stores/me/locations — StorePartner gate ──────────────────────
    [HttpGet("me/locations")]
    [Authorize(Roles = "Store")]
    [EnableRateLimiting("public-api")]
    public async Task<IActionResult> GetLocations(CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await sender.Send(new GetMyStoreLocationsQuery(userId), ct);
        if (result.IsFailure)
            return UnprocessableEntity(new ProblemDetails { Detail = string.Join("; ", result.Errors), Status = 422 });
        return Ok(result.Value);
    }

    // ── POST /api/stores/me/locations — StorePartner gate ─────────────────────
    [HttpPost("me/locations")]
    [Authorize(Roles = "Store")]
    [EnableRateLimiting("public-api")]
    [RequestSizeLimit(2048)]
    public async Task<IActionResult> CreateLocation(
        [FromBody] StoreLocationRequest request, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await sender.Send(new CreateStoreLocationCommand(
            userId, request.Name, request.Address, request.Lat, request.Lng, request.PhoneNumber), ct);
        if (result.IsFailure)
            return UnprocessableEntity(new ProblemDetails { Detail = string.Join("; ", result.Errors), Status = 422 });
        return Created(string.Empty, result.Value);
    }

    // ── PUT /api/stores/me/locations/{id} — StorePartner gate ─────────────────
    [HttpPut("me/locations/{locationId:guid}")]
    [Authorize(Roles = "Store")]
    [EnableRateLimiting("public-api")]
    [RequestSizeLimit(2048)]
    public async Task<IActionResult> UpdateLocation(
        Guid locationId, [FromBody] StoreLocationRequest request, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await sender.Send(new UpdateStoreLocationCommand(
            userId, locationId, request.Name, request.Address, request.Lat, request.Lng, request.PhoneNumber), ct);
        if (result.IsFailure)
            return UnprocessableEntity(new ProblemDetails { Detail = string.Join("; ", result.Errors), Status = 422 });
        return Ok(result.Value);
    }

    // ── PATCH /api/stores/me/locations/{id}/active — StorePartner gate ────────
    [HttpPatch("me/locations/{locationId:guid}/active")]
    [Authorize(Roles = "Store")]
    [EnableRateLimiting("public-api")]
    [RequestSizeLimit(256)]
    public async Task<IActionResult> SetLocationActive(
        Guid locationId, [FromBody] SetLocationActiveRequest request, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await sender.Send(new SetStoreLocationActiveCommand(userId, locationId, request.Active), ct);
        if (result.IsFailure)
            return UnprocessableEntity(new ProblemDetails { Detail = string.Join("; ", result.Errors), Status = 422 });
        return Ok(result.Value);
    }

    // ── GET /api/stores/products — owner's full catalog ───────────────────────
    [HttpGet("products")]
    [Authorize(Roles = "Store")]
    [EnableRateLimiting("public-api")]
    public async Task<IActionResult> GetProducts(CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await sender.Send(new GetMyStoreProductsQuery(userId), ct);
        return result.IsSuccess ? Ok(result.Value) : UnprocessableEntity(result.Errors);
    }

    // ── POST /api/stores/products ─────────────────────────────────────────────
    [HttpPost("products")]
    [Authorize(Roles = "Store")]
    [EnableRateLimiting("public-api")]
    [RequestSizeLimit(2048)]
    public async Task<IActionResult> AddProduct(
        [FromBody] AddProductRequest request,
        CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        if (!Enum.TryParse<ProductCategory>(request.Category, ignoreCase: true, out var category))
            return BadRequest(new ProblemDetails { Detail = $"Categoría inválida: {request.Category}.", Status = 400 });

        var result = await sender.Send(new AddStoreProductCommand(
            userId, request.Name, request.Description, category, request.PriceCrc), ct);
        if (result.IsFailure)
            return UnprocessableEntity(new ProblemDetails { Detail = string.Join("; ", result.Errors), Status = 422 });
        return Created(string.Empty, result.Value);
    }

    // ── PUT /api/stores/products/{id} ─────────────────────────────────────────
    [HttpPut("products/{productId:guid}")]
    [Authorize(Roles = "Store")]
    [EnableRateLimiting("public-api")]
    [RequestSizeLimit(2048)]
    public async Task<IActionResult> UpdateProduct(
        Guid productId,
        [FromBody] UpdateProductRequest request,
        CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        if (!Enum.TryParse<ProductCategory>(request.Category, ignoreCase: true, out var category))
            return BadRequest(new ProblemDetails { Detail = $"Categoría inválida: {request.Category}.", Status = 400 });

        var result = await sender.Send(new UpdateStoreProductCommand(
            userId, productId, request.Name, request.Description, category, request.PriceCrc, request.IsAvailable), ct);
        if (result.IsFailure)
            return UnprocessableEntity(new ProblemDetails { Detail = string.Join("; ", result.Errors), Status = 422 });
        return Ok(result.Value);
    }

    // ── DELETE /api/stores/products/{id} ──────────────────────────────────────
    [HttpDelete("products/{productId:guid}")]
    [Authorize(Roles = "Store")]
    [EnableRateLimiting("public-api")]
    public async Task<IActionResult> DeleteProduct(Guid productId, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await sender.Send(new DeleteStoreProductCommand(userId, productId), ct);
        if (result.IsFailure)
            return UnprocessableEntity(new ProblemDetails { Detail = string.Join("; ", result.Errors), Status = 422 });
        return NoContent();
    }

    // ── POST /api/stores/products/{id}/image — upload product photo ───────────
    [HttpPost("products/{productId:guid}/image")]
    [Authorize(Roles = "Store")]
    [EnableRateLimiting("public-api")]
    [RequestSizeLimit(5_242_880)] // 5 MB
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> UploadProductImage(
        Guid productId,
        [FromForm] IFormFile? image,
        CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        if (image is null || image.Length == 0)
            return BadRequest(new ProblemDetails { Detail = "Se requiere una imagen.", Status = 400 });
        var allowed = new[] { "image/jpeg", "image/png", "image/webp" };
        if (!allowed.Contains(image.ContentType, StringComparer.OrdinalIgnoreCase))
            return BadRequest(new ProblemDetails { Detail = "Solo se aceptan JPEG, PNG o WebP.", Status = 400 });

        using var ms = new MemoryStream();
        await image.CopyToAsync(ms, ct);
        var result = await sender.Send(new UploadProductImageCommand(userId, productId, ms.ToArray(), image.ContentType), ct);
        if (result.IsFailure)
            return UnprocessableEntity(new ProblemDetails { Detail = string.Join("; ", result.Errors), Status = 422 });
        return Ok(result.Value);
    }

    private bool TryGetUserId(out Guid userId)
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(claim, out userId);
    }
}

// ── Public stores endpoint (no auth) ─────────────────────────────────────────

[ApiController]
[Route("api/public/stores")]
[EnableRateLimiting("public-api")]
public sealed class PublicStoresController(ISender sender) : ControllerBase
{
    // ── GET /api/public/stores ────────────────────────────────────────────────
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var result = await sender.Send(new GetPublicStoresQuery(page, Math.Clamp(pageSize, 1, 100)), ct);
        return result.IsSuccess ? Ok(result.Value) : Ok(Array.Empty<object>());
    }

    // ── GET /api/public/stores/{id} ───────────────────────────────────────────
    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetDetail(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new GetStoreDetailQuery(id), ct);
        if (result.IsFailure) return NotFound(new ProblemDetails { Title = "Tienda no encontrada", Status = 404 });
        return Ok(result.Value);
    }
}

// ── Request models ────────────────────────────────────────────────────────────

public sealed record RegisterStoreRequest(
    string Name, string Description, string Address,
    decimal Lat, decimal Lng, string ContactEmail, string Password);

public sealed record UpdateStoreProfileRequest(
    string Name, string Description, string Address,
    decimal Lat, decimal Lng, string? PhoneNumber, string? Website);

public sealed record AddProductRequest(
    string Name, string? Description, string Category, decimal PriceCrc);

public sealed record UpdateProductRequest(
    string Name, string? Description, string Category, decimal PriceCrc, bool IsAvailable);

public sealed record StoreLocationRequest(
    string Name, string Address, decimal Lat, decimal Lng, string? PhoneNumber);

public sealed record SetLocationActiveRequest(bool Active);
