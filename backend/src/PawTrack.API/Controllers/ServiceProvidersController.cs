using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PawTrack.Application.ServiceProviders;
using PawTrack.Domain.ServiceProviders;
using System.Security.Claims;

namespace PawTrack.API.Controllers;

[ApiController]
[Route("api/service-providers")]
public sealed class ServiceProvidersController(ISender sender) : ControllerBase
{
    [HttpPost("register")]
    [AllowAnonymous]
    [EnableRateLimiting("register")]
    [RequestSizeLimit(2048)]
    public async Task<IActionResult> Register([FromBody] RegisterServiceProviderRequest request, CancellationToken ct)
    {
        if (!Enum.TryParse<ServiceProviderCategory>(request.Category, true, out var category))
            return BadRequest(new ProblemDetails { Detail = "Categoria invalida.", Status = StatusCodes.Status400BadRequest });

        var result = await sender.Send(new RegisterServiceProviderCommand(
            request.Name, request.Description, category, request.Address, request.Lat, request.Lng,
            request.ContactEmail, request.Password), ct);
        if (result.IsFailure)
        {
            if (result.Errors.Contains(RegisterServiceProviderCommandHandler.DuplicateEmailError))
                return Created(string.Empty, new { message = "Solicitud recibida. Si es elegible, recibira confirmacion." });
            return UnprocessableEntity(new ProblemDetails { Detail = string.Join("; ", result.Errors), Status = 422 });
        }

        return Created(string.Empty, result.Value);
    }

    [HttpGet("mine")]
    [Authorize(Roles = "ServiceProvider")]
    [EnableRateLimiting("public-api")]
    public async Task<IActionResult> GetMine(CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await sender.Send(new GetMyServiceProviderQuery(userId), ct);
        return result.IsSuccess ? Ok(result.Value) : NotFound(result.Errors);
    }

    [HttpPut("profile")]
    [Authorize(Roles = "ServiceProvider")]
    [EnableRateLimiting("public-api")]
    [RequestSizeLimit(2048)]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateServiceProviderProfileRequest request, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        if (!Enum.TryParse<ServiceProviderCategory>(request.Category, true, out var category))
            return BadRequest(new ProblemDetails { Detail = "Categoria invalida.", Status = StatusCodes.Status400BadRequest });

        var result = await sender.Send(new UpdateServiceProviderProfileCommand(
            userId, request.Name, request.Description, category, request.Address, request.Lat,
            request.Lng, request.PhoneNumber, request.Website), ct);
        return result.IsSuccess
            ? Ok(result.Value)
            : UnprocessableEntity(new ProblemDetails { Detail = string.Join("; ", result.Errors), Status = 422 });
    }

    [HttpGet("services")]
    [Authorize(Roles = "ServiceProvider")]
    [EnableRateLimiting("public-api")]
    public async Task<IActionResult> GetServices(CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await sender.Send(new GetMyProviderServicesQuery(userId), ct);
        return result.IsSuccess ? Ok(result.Value) : UnprocessableEntity(result.Errors);
    }

    [HttpPost("services")]
    [Authorize(Roles = "ServiceProvider")]
    [EnableRateLimiting("public-api")]
    [RequestSizeLimit(2048)]
    public async Task<IActionResult> AddService([FromBody] AddProviderServiceRequest request, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        if (!Enum.TryParse<ServiceModality>(request.Modality, true, out var modality))
            return BadRequest(new ProblemDetails { Detail = "Modalidad invalida.", Status = StatusCodes.Status400BadRequest });

        var result = await sender.Send(new AddProviderServiceCommand(
            userId, request.Name, request.Description, modality, request.DurationMinutes,
            request.PriceCrc, request.Capacity), ct);
        return result.IsSuccess
            ? Created(string.Empty, result.Value)
            : UnprocessableEntity(new ProblemDetails { Detail = string.Join("; ", result.Errors), Status = 422 });
    }

    [HttpPut("services/{serviceId:guid}/status")]
    [Authorize(Roles = "ServiceProvider")]
    [EnableRateLimiting("public-api")]
    [RequestSizeLimit(256)]
    public async Task<IActionResult> SetServiceStatus(
        Guid serviceId,
        [FromBody] SetProviderServiceStatusRequest request,
        CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        if (!Enum.TryParse<ProviderServiceStatus>(request.Status, true, out var status))
            return BadRequest(new ProblemDetails { Detail = "Estado de servicio invalido.", Status = StatusCodes.Status400BadRequest });
        var result = await sender.Send(new SetProviderServiceStatusCommand(userId, serviceId, status), ct);
        return result.IsSuccess
            ? Ok(result.Value)
            : UnprocessableEntity(new ProblemDetails { Detail = string.Join("; ", result.Errors), Status = 422 });
    }

    [HttpPut("services/{serviceId:guid}")]
    [Authorize(Roles = "ServiceProvider")]
    [EnableRateLimiting("public-api")]
    [RequestSizeLimit(2048)]
    public async Task<IActionResult> UpdateService(
        Guid serviceId,
        [FromBody] UpdateProviderServiceRequest request,
        CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        if (!Enum.TryParse<ServiceModality>(request.Modality, true, out var modality))
            return BadRequest(new ProblemDetails { Detail = "Modalidad invalida.", Status = StatusCodes.Status400BadRequest });
        var result = await sender.Send(new UpdateProviderServiceCommand(
            userId, serviceId, request.Name, request.Description, modality, request.DurationMinutes, request.PriceCrc, request.Capacity), ct);
        return result.IsSuccess
            ? Ok(result.Value)
            : UnprocessableEntity(new ProblemDetails { Detail = string.Join("; ", result.Errors), Status = 422 });
    }

    [HttpPost("services/{serviceId:guid}/availability")]
    [Authorize(Roles = "ServiceProvider")]
    [EnableRateLimiting("public-api")]
    [RequestSizeLimit(1024)]
    public async Task<IActionResult> AddAvailabilityRule(
        Guid serviceId,
        [FromBody] AddServiceAvailabilityRuleRequest request,
        CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        if (!Enum.IsDefined(request.DayOfWeek))
            return BadRequest(new ProblemDetails { Detail = "Dia de la semana invalido.", Status = StatusCodes.Status400BadRequest });

        var result = await sender.Send(new AddServiceAvailabilityRuleCommand(
            userId, serviceId, request.DayOfWeek, request.StartsAtLocalTime, request.EndsAtLocalTime), ct);
        return result.IsSuccess
            ? Created(string.Empty, new { id = result.Value })
            : UnprocessableEntity(new ProblemDetails { Detail = string.Join("; ", result.Errors), Status = 422 });
    }

    [HttpGet("services/{serviceId:guid}/availability")]
    [Authorize(Roles = "ServiceProvider")]
    [EnableRateLimiting("public-api")]
    public async Task<IActionResult> GetAvailabilityRules(Guid serviceId, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await sender.Send(new GetServiceAvailabilityRulesQuery(userId, serviceId), ct);
        return result.IsSuccess ? Ok(result.Value) : UnprocessableEntity(result.Errors);
    }

    [HttpDelete("availability/{ruleId:guid}")]
    [Authorize(Roles = "ServiceProvider")]
    [EnableRateLimiting("public-api")]
    public async Task<IActionResult> DeactivateAvailabilityRule(Guid ruleId, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await sender.Send(new DeactivateServiceAvailabilityRuleCommand(userId, ruleId), ct);
        return result.IsSuccess ? NoContent() : UnprocessableEntity(result.Errors);
    }

    [HttpPost("services/{serviceId:guid}/availability-blocks")]
    [Authorize(Roles = "ServiceProvider")]
    [EnableRateLimiting("public-api")]
    [RequestSizeLimit(1024)]
    public async Task<IActionResult> AddAvailabilityBlock(
        Guid serviceId,
        [FromBody] AddServiceAvailabilityBlockRequest request,
        CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await sender.Send(new AddServiceAvailabilityBlockCommand(
            userId, serviceId, request.StartsAt, request.EndsAt, request.Reason), ct);
        return result.IsSuccess
            ? Created(string.Empty, new { id = result.Value })
            : UnprocessableEntity(new ProblemDetails { Detail = string.Join("; ", result.Errors), Status = 422 });
    }

    [HttpGet("services/{serviceId:guid}/availability-blocks")]
    [Authorize(Roles = "ServiceProvider")]
    [EnableRateLimiting("public-api")]
    public async Task<IActionResult> GetAvailabilityBlocks(
        Guid serviceId,
        [FromQuery] DateTimeOffset from,
        [FromQuery] DateTimeOffset to,
        CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await sender.Send(new GetServiceAvailabilityBlocksQuery(userId, serviceId, from, to), ct);
        return result.IsSuccess
            ? Ok(result.Value)
            : UnprocessableEntity(new ProblemDetails { Detail = string.Join("; ", result.Errors), Status = 422 });
    }

    [HttpDelete("availability-blocks/{blockId:guid}")]
    [Authorize(Roles = "ServiceProvider")]
    [EnableRateLimiting("public-api")]
    public async Task<IActionResult> DeactivateAvailabilityBlock(Guid blockId, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await sender.Send(new DeactivateServiceAvailabilityBlockCommand(userId, blockId), ct);
        return result.IsSuccess ? NoContent() : UnprocessableEntity(result.Errors);
    }

    [HttpPost("bookings/{bookingId:guid}/payment")]
    [Authorize(Roles = "Owner")]
    [EnableRateLimiting("public-api")]
    public async Task<IActionResult> CreateBookingPayment(
        Guid bookingId,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        if (string.IsNullOrWhiteSpace(idempotencyKey))
            return BadRequest(new ProblemDetails { Detail = "Se requiere Idempotency-Key.", Status = 400 });
        var result = await sender.Send(new CreateProviderBookingPaymentCommand(userId, bookingId, idempotencyKey), ct);
        return result.IsSuccess
            ? Ok(result.Value)
            : UnprocessableEntity(new ProblemDetails { Detail = string.Join("; ", result.Errors), Status = 422 });
    }

    [HttpPost("payments/{paymentId:guid}/report")]
    [Authorize(Roles = "Owner")]
    [EnableRateLimiting("public-api")]
    public async Task<IActionResult> ReportBookingPayment(Guid paymentId, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await sender.Send(new ReportProviderBookingPaymentCommand(userId, paymentId), ct);
        return result.IsSuccess
            ? Ok(result.Value)
            : UnprocessableEntity(new ProblemDetails { Detail = string.Join("; ", result.Errors), Status = 422 });
    }

    [HttpGet("incidents")]
    [Authorize(Roles = "ServiceProvider")]
    [EnableRateLimiting("public-api")]
    public async Task<IActionResult> GetMyIncidents([FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await sender.Send(new GetMyProviderIncidentsQuery(userId, page, pageSize), ct);
        return result.IsSuccess ? Ok(result.Value) : NotFound(new ProblemDetails { Detail = string.Join("; ", result.Errors), Status = 404 });
    }

    [HttpPost("incidents/{serviceProviderId:guid}")]
    [Authorize(Roles = "ServiceProvider")]
    [EnableRateLimiting("public-api")]
    public async Task<IActionResult> OpenIncident(Guid serviceProviderId, [FromBody] OpenProviderIncidentRequest request, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        if (!Enum.TryParse<ProviderIncidentType>(request.Type, true, out var type))
            return BadRequest(new ProblemDetails { Detail = "Tipo de incidente invalido.", Status = 400 });
        var result = await sender.Send(new OpenProviderIncidentCommand(userId, serviceProviderId, type, request.Description, request.EvidenceReference), ct);
        return result.IsSuccess ? Created(string.Empty, result.Value) : UnprocessableEntity(new ProblemDetails { Detail = string.Join("; ", result.Errors), Status = 422 });
    }

    [HttpPost("verification/document")]
    [Authorize(Roles = "ServiceProvider")]
    [EnableRateLimiting("public-api")]
    [RequestSizeLimit(5_242_880)]
    public async Task<IActionResult> UploadVerificationDocument(IFormFile? file, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        if (file is null || file.Length == 0)
            return BadRequest(new ProblemDetails { Detail = "Se requiere un documento.", Status = StatusCodes.Status400BadRequest });

        var allowed = new[] { "application/pdf", "image/jpeg", "image/png", "image/webp" };
        if (!allowed.Contains(file.ContentType, StringComparer.OrdinalIgnoreCase))
            return BadRequest(new ProblemDetails { Detail = "Solo se aceptan PDF, JPEG, PNG o WebP.", Status = StatusCodes.Status400BadRequest });

        using var stream = new MemoryStream();
        await file.CopyToAsync(stream, ct);
        var result = await sender.Send(new UploadProviderVerificationDocumentCommand(userId, stream.ToArray(), file.ContentType), ct);
        return result.IsSuccess
            ? Created(string.Empty, result.Value)
            : UnprocessableEntity(new ProblemDetails { Detail = string.Join("; ", result.Errors), Status = 422 });
    }

    [HttpGet("verification")]
    [Authorize(Roles = "ServiceProvider")]
    [EnableRateLimiting("public-api")]
    public async Task<IActionResult> GetVerification(CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await sender.Send(new GetMyProviderVerificationQuery(userId), ct);
        return result.IsSuccess ? Ok(result.Value) : UnprocessableEntity(result.Errors);
    }

    [HttpGet("verification/document")]
    [Authorize(Roles = "ServiceProvider")]
    [EnableRateLimiting("public-api")]
    public async Task<IActionResult> DownloadVerificationDocument(CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var current = await sender.Send(new GetMyProviderVerificationQuery(userId), ct);
        if (current.IsFailure || current.Value is null)
            return NotFound(new ProblemDetails { Detail = "Documento no disponible.", Status = StatusCodes.Status404NotFound });
        var result = await sender.Send(new DownloadProviderVerificationDocumentQuery(current.Value.Id, userId, false), ct);
        return result.IsSuccess
            ? File(result.Value!.Bytes, result.Value.ContentType, result.Value.FileName)
            : UnprocessableEntity(new ProblemDetails { Detail = string.Join("; ", result.Errors), Status = 422 });
    }

    private bool TryGetUserId(out Guid userId)
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(claim, out userId);
    }
}

public sealed record OpenProviderIncidentRequest(string Type, string Description, string? EvidenceReference);

[ApiController]
[Route("api/public/service-providers")]
[EnableRateLimiting("public-api")]
public sealed class PublicServiceProvidersController(ISender sender) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? category,
        [FromQuery] string? modality,
        [FromQuery] decimal? minPriceCrc,
        [FromQuery] decimal? maxPriceCrc,
        [FromQuery] decimal? centerLat,
        [FromQuery] decimal? centerLng,
        [FromQuery] int? radiusKm,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        ServiceProviderCategory? selectedCategory = null;
        if (category is not null)
        {
            if (!Enum.TryParse<ServiceProviderCategory>(category, true, out var parsedCategory))
                return BadRequest(new ProblemDetails { Detail = "Categoria invalida.", Status = StatusCodes.Status400BadRequest });

            selectedCategory = parsedCategory;
        }
        ServiceModality? selectedModality = null;
        if (modality is not null)
        {
            if (!Enum.TryParse<ServiceModality>(modality, true, out var parsedModality))
                return BadRequest(new ProblemDetails { Detail = "Modalidad invalida.", Status = StatusCodes.Status400BadRequest });
            selectedModality = parsedModality;
        }
        var result = await sender.Send(new GetPublicServiceProvidersQuery(
            selectedCategory, selectedModality, minPriceCrc, maxPriceCrc, centerLat, centerLng, radiusKm, page, pageSize), ct);
        return result.IsSuccess
            ? Ok(result.Value)
            : BadRequest(new ProblemDetails { Detail = string.Join("; ", result.Errors), Status = StatusCodes.Status400BadRequest });
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetDetail(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new GetServiceProviderDetailQuery(id), ct);
        return result.IsSuccess
            ? Ok(result.Value)
            : NotFound(new ProblemDetails { Title = "Proveedor no encontrado", Status = StatusCodes.Status404NotFound });
    }

    [HttpGet("{id:guid}/services")]
    [AllowAnonymous]
    public async Task<IActionResult> GetServices(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new GetPublicProviderServicesQuery(id), ct);
        return result.IsSuccess
            ? Ok(result.Value)
            : NotFound(new ProblemDetails { Title = "Proveedor no encontrado", Status = StatusCodes.Status404NotFound });
    }
}

public sealed record RegisterServiceProviderRequest(
    string Name,
    string Description,
    string Category,
    string Address,
    decimal Lat,
    decimal Lng,
    string ContactEmail,
    string Password);

public sealed record UpdateServiceProviderProfileRequest(
    string Name,
    string Description,
    string Category,
    string Address,
    decimal Lat,
    decimal Lng,
    string? PhoneNumber,
    string? Website);

public sealed record AddProviderServiceRequest(
    string Name,
    string Description,
    string Modality,
    int DurationMinutes,
    decimal PriceCrc,
    int Capacity);

public sealed record SetProviderServiceStatusRequest(string Status);

public sealed record UpdateProviderServiceRequest(
    string Name,
    string Description,
    string Modality,
    int DurationMinutes,
    decimal PriceCrc,
    int Capacity);

public sealed record AddServiceAvailabilityRuleRequest(
    DayOfWeek DayOfWeek,
    TimeOnly StartsAtLocalTime,
    TimeOnly EndsAtLocalTime);

public sealed record AddServiceAvailabilityBlockRequest(DateTimeOffset StartsAt, DateTimeOffset EndsAt, string Reason);