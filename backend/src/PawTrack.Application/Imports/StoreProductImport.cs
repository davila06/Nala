using System.Globalization;
using System.Text;
using System.Text.Json;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Subscriptions.Services;
using PawTrack.Domain.Imports;
using PawTrack.Domain.Stores;

namespace PawTrack.Application.Imports;

public sealed class StoreProductImportProcessor(
    IImportJobRepository importRepository,
    IStoreRepository storeRepository,
    IServiceProviderRepository providerRepository,
    IEntitlementService entitlementService,
    IUnitOfWork unitOfWork)
{
    public async Task<ImportJob> ProcessAsync(
        Guid tenantId,
        string format,
        string fileHash,
        string idempotencyKey,
        byte[] payload,
        CancellationToken cancellationToken)
    {
        var existing = await importRepository.GetByIdempotencyKeyAsync(tenantId, idempotencyKey, cancellationToken);
        if (existing is not null && existing.Status is not (ImportJobStatus.Pending or ImportJobStatus.Processing)) return existing;

        var rows = ParseRows(payload, format).ToList();
        var job = existing ?? ImportJob.Create(tenantId, "StoreProducts", format, fileHash, idempotencyKey, rows.Count);
        if (existing is null) await importRepository.AddAsync(job, cancellationToken);
        job.Start();

        var store = await storeRepository.GetByUserIdAsync(tenantId, cancellationToken);
        if (store is null)
        {
            job.AddError(0, "store", "STORE_NOT_FOUND", "No existe una tienda para el tenant.", null);
            job.Fail();
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return job;
        }

        var products = await storeRepository.GetProductsByStoreAsync(store.Id, cancellationToken);
        var decision = await entitlementService.AuthorizeAsync(
            tenantId, "MaxActiveProducts", 1m,
            new EntitlementContext("store-product-import", store.Id), cancellationToken);
        var limit = decision.Limit ?? products.Count + rows.Count;
        var activeCount = products.Count(product => product.IsAvailable);

        for (var index = 0; index < rows.Count; index++)
        {
            var row = rows[index];
            var line = index + 2;
            if (!row.TryGetValue("name", out var name) || string.IsNullOrWhiteSpace(name))
            {
                job.AddError(line, "name", "REQUIRED", "Name es requerido.", null);
                continue;
            }
            if (!decimal.TryParse(row.GetValueOrDefault("priceCrc"), NumberStyles.Number, CultureInfo.InvariantCulture, out var price) || price <= 0)
            {
                job.AddError(line, "priceCrc", "INVALID_DECIMAL", "priceCrc debe ser mayor que cero.", row.GetValueOrDefault("priceCrc"));
                continue;
            }
            if (activeCount >= limit)
            {
                job.AddError(line, "name", "PLAN_LIMIT", "El plan no permite más productos activos.", name);
                continue;
            }
            if (!Enum.TryParse<ProductCategory>(row.GetValueOrDefault("category"), true, out var category))
                category = ProductCategory.Other;

            var product = StoreProduct.Create(store.Id, name, row.GetValueOrDefault("description"), category, price);
            await storeRepository.AddProductAsync(product, cancellationToken);
            activeCount++;
            job.AddImported();
        }

        job.Complete();
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return job;
    }

    public async Task<ImportJob> ProcessProviderServicesAsync(
        Guid tenantId, string format, string fileHash, string idempotencyKey,
        byte[] payload, CancellationToken cancellationToken)
    {
        var existing = await importRepository.GetByIdempotencyKeyAsync(tenantId, idempotencyKey, cancellationToken);
        if (existing is not null && existing.Status is not (ImportJobStatus.Pending or ImportJobStatus.Processing)) return existing;
        var rows = ParseRows(payload, format).ToList();
        var job = existing ?? ImportJob.Create(tenantId, "ProviderServices", format, fileHash, idempotencyKey, rows.Count);
        if (existing is null) await importRepository.AddAsync(job, cancellationToken);
        job.Start();
        var provider = await providerRepository.GetByUserIdAsync(tenantId, cancellationToken);
        if (provider is null)
        {
            job.AddError(0, "provider", "PROVIDER_NOT_FOUND", "No existe un proveedor para el tenant.", null);
            job.Fail();
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return job;
        }
        var services = await providerRepository.GetServicesByProviderAsync(provider.Id, cancellationToken);
        var membershipLimit = provider.MembershipTier switch
        {
            Domain.ServiceProviders.ProviderMembershipTier.Verified => 25m,
            Domain.ServiceProviders.ProviderMembershipTier.Featured => 100m,
            _ => 3m,
        };
        for (var index = 0; index < rows.Count; index++)
        {
            var row = rows[index];
            var line = index + 2;
            if (!row.TryGetValue("name", out var name) || string.IsNullOrWhiteSpace(name))
            { job.AddError(line, "name", "REQUIRED", "Name es requerido.", null); continue; }
            if (!int.TryParse(row.GetValueOrDefault("durationMinutes"), out var duration) || duration is < 15 or > 1440)
            { job.AddError(line, "durationMinutes", "INVALID_DURATION", "Duración inválida.", row.GetValueOrDefault("durationMinutes")); continue; }
            if (!decimal.TryParse(row.GetValueOrDefault("priceCrc"), NumberStyles.Number, CultureInfo.InvariantCulture, out var price) || price < 0)
            { job.AddError(line, "priceCrc", "INVALID_DECIMAL", "priceCrc inválido.", row.GetValueOrDefault("priceCrc")); continue; }
            if (!int.TryParse(row.GetValueOrDefault("capacity"), out var capacity) || capacity is < 1 or > 100)
            { job.AddError(line, "capacity", "INVALID_CAPACITY", "Capacity inválida.", row.GetValueOrDefault("capacity")); continue; }
            if (services.Count(service => service.Status != Domain.ServiceProviders.ProviderServiceStatus.Archived) >= membershipLimit)
            { job.AddError(line, "name", "PLAN_LIMIT", "El proveedor alcanzó el límite de servicios.", name); continue; }
            Enum.TryParse(row.GetValueOrDefault("modality"), true, out Domain.ServiceProviders.ServiceModality modality);
            var service = Domain.ServiceProviders.ProviderService.Create(
                provider.Id, name, row.GetValueOrDefault("description") ?? string.Empty,
                modality, duration, price, capacity);
            await providerRepository.AddServiceAsync(service, cancellationToken);
            services = services.Append(service).ToList();
            job.AddImported();
        }
        job.Complete();
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return job;
    }

    private static IEnumerable<Dictionary<string, string?>> ParseRows(byte[] payload, string format)
    {
        if (format == "json")
        {
            using var document = JsonDocument.Parse(payload);
            var elements = document.RootElement.ValueKind == JsonValueKind.Array
                ? document.RootElement.EnumerateArray().ToList()
                : new List<JsonElement> { document.RootElement };
            foreach (var element in elements)
                yield return element.EnumerateObject().ToDictionary(
                    property => property.Name, property => (string?)property.Value.ToString(), StringComparer.OrdinalIgnoreCase);
            yield break;
        }

        using var reader = new StringReader(Encoding.UTF8.GetString(payload));
        var headers = ParseCsvLine(reader.ReadLine() ?? string.Empty);
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            var values = ParseCsvLine(line);
            yield return headers.Select((header, index) =>
                new { header, value = index < values.Count ? values[index] : null })
                .ToDictionary(item => item.header, item => item.value, StringComparer.OrdinalIgnoreCase);
        }
    }

    private static List<string> ParseCsvLine(string line)
    {
        var values = new List<string>();
        var current = new StringBuilder();
        var quoted = false;
        for (var index = 0; index < line.Length; index++)
        {
            var character = line[index];
            if (character == '"')
            {
                if (quoted && index + 1 < line.Length && line[index + 1] == '"') { current.Append('"'); index++; }
                else quoted = !quoted;
            }
            else if (character == ',' && !quoted) { values.Add(current.ToString().Trim()); current.Clear(); }
            else current.Append(character);
        }
        values.Add(current.ToString().Trim());
        return values;
    }
}
