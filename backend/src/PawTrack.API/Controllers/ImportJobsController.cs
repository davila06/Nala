using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PawTrack.Application.Imports;
using PawTrack.Application.Subscriptions.Services;
using PawTrack.Domain.Imports;

namespace PawTrack.API.Controllers;

[ApiController]
[Route("api/imports")]
[Authorize]
public sealed class ImportJobsController(
    IImportJobRepository repository,
    StoreProductImportProcessor storeProductImportProcessor,
    IEntitlementService? entitlementService = null) : ControllerBase
{
    private const long MaxFileBytes = 5 * 1024 * 1024;
    private const int TechnicalMaxRows = 10_000;

    [HttpPost]
    [RequestSizeLimit(MaxFileBytes)]
    [ProducesResponseType(typeof(ImportJobResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Create(
        [FromForm] IFormFile file,
        [FromForm] string resourceType,
        CancellationToken cancellationToken)
    {
        if (!TryGetTenantId(out var tenantId)) return Unauthorized();
        if (file is null || file.Length == 0)
            return UnprocessableEntity(new ProblemDetails { Detail = "El archivo es requerido." });
        if (file.Length > MaxFileBytes)
            return UnprocessableEntity(new ProblemDetails { Detail = "El archivo excede 5 MB." });
        if (resourceType is not ("StoreProducts" or "ProviderServices"))
            return UnprocessableEntity(new ProblemDetails { Detail = "El tipo de recurso no está soportado." });

        var format = Path.GetExtension(file.FileName).ToLowerInvariant() switch
        {
            ".csv" => "csv",
            ".json" => "json",
            _ => string.Empty,
        };
        if (format.Length == 0)
            return UnprocessableEntity(new ProblemDetails { Detail = "Solo se aceptan archivos CSV o JSON." });

        var idempotencyKey = Request.Headers["Idempotency-Key"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(idempotencyKey))
            return UnprocessableEntity(new ProblemDetails { Detail = "Idempotency-Key es requerido." });

        await using var stream = file.OpenReadStream();
        using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer, cancellationToken);
        var bytes = buffer.ToArray();
        var rowCount = CountRows(bytes, format);
        if (rowCount > TechnicalMaxRows)
            return UnprocessableEntity(new ProblemDetails { Detail = "La importación excede el límite técnico de 10.000 filas." });
        if (entitlementService is not null)
        {
            var decision = await entitlementService.AuthorizeAsync(
                tenantId, "BulkImportLimit", rowCount,
                new EntitlementContext("bulk-import", tenantId), cancellationToken);
            if (!decision.Allowed)
                return UnprocessableEntity(new ProblemDetails { Detail = "La importación excede la cuota del plan." });
        }

        var existing = await repository.GetByIdempotencyKeyAsync(tenantId, idempotencyKey, cancellationToken);
        if (existing is not null)
            return Accepted(new ImportJobResponse(existing.Id, existing.Status.ToString(), existing.RowCount, existing.ImportedCount, existing.DuplicateCount));

        var hash = Convert.ToHexString(SHA256.HashData(bytes));
        var job = resourceType == "StoreProducts"
            ? await storeProductImportProcessor.ProcessAsync(tenantId, format, hash, idempotencyKey, bytes, cancellationToken)
            : await storeProductImportProcessor.ProcessProviderServicesAsync(tenantId, format, hash, idempotencyKey, bytes, cancellationToken);
        return Accepted(new ImportJobResponse(job.Id, job.Status.ToString(), job.RowCount, job.ImportedCount, job.DuplicateCount));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetTenantId(out var tenantId)) return Unauthorized();
        var job = await repository.GetByIdAsync(tenantId, id, cancellationToken);
        return job is null ? NotFound() : Ok(ToResponse(job));
    }

    [HttpGet("{id:guid}/errors")]
    public async Task<IActionResult> GetErrors(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetTenantId(out var tenantId)) return Unauthorized();
        var job = await repository.GetByIdAsync(tenantId, id, cancellationToken);
        return job is null ? NotFound() : Ok(job.Errors);
    }

    private bool TryGetTenantId(out Guid tenantId) =>
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out tenantId);

    private static ImportJobResponse ToResponse(ImportJob job) =>
        new(job.Id, job.Status.ToString(), job.RowCount, job.ImportedCount, job.DuplicateCount);

    private static int CountRows(byte[] bytes, string format)
    {
        if (format == "json")
        {
            using var document = JsonDocument.Parse(bytes);
            return document.RootElement.ValueKind == JsonValueKind.Array
                ? document.RootElement.GetArrayLength()
                : 1;
        }

        var text = System.Text.Encoding.UTF8.GetString(bytes);
        return Math.Max(0, text.Split('\n', StringSplitOptions.RemoveEmptyEntries).Length - 1);
    }
}

public sealed record ImportJobResponse(
    Guid Id,
    string Status,
    int RowCount,
    int ImportedCount,
    int DuplicateCount);
