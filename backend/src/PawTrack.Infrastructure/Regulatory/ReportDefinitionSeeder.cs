using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PawTrack.Domain.Regulatory;
using PawTrack.Infrastructure.Persistence;

namespace PawTrack.Infrastructure.Regulatory;

public static class ReportDefinitionSeeder
{
    public static async Task SeedAsync(IServiceProvider services, ILogger logger, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PawTrackDbContext>();
        var existingCodes = await dbContext.ReportDefinitions
            .Select(definition => definition.Code)
            .ToHashSetAsync(cancellationToken);

        var definitions = Definitions()
            .Where(definition => !existingCodes.Contains(definition.Code))
            .ToList();
        if (definitions.Count == 0) return;

        await dbContext.ReportDefinitions.AddRangeAsync(definitions, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Seeded {Count} Regulatory report definitions", definitions.Count);
    }

    private static IEnumerable<ReportDefinition> Definitions()
    {
        yield return ReportDefinition.Create(ReportType.Recovery, "RECOVERY_SUMMARY", "Recuperación de mascotas", "1.0", ExportScope.Institutional, 5, 90);
        yield return ReportDefinition.Create(ReportType.MunicipalCaptures, "MUNICIPAL_CAPTURES", "Capturas municipales", "1.0", ExportScope.Institutional, 5, 90);
        yield return ReportDefinition.Create(ReportType.Adoptions, "ADOPTION_SUMMARY", "Adopciones", "1.0", ExportScope.Institutional, 5, 90);
        yield return ReportDefinition.Create(ReportType.WelfareCases, "WELFARE_CASES", "Casos de bienestar animal", "1.0", ExportScope.Institutional, 5, 90);
        yield return ReportDefinition.Create(ReportType.SanitaryIdentity, "SANITARY_IDENTITY", "Identidad sanitaria agregada", "1.0", ExportScope.Institutional, 5, 90);
        yield return ReportDefinition.Create(ReportType.NetworkCoverage, "NETWORK_COVERAGE", "Cobertura de red", "1.0", ExportScope.Institutional, 5, 90);
        yield return ReportDefinition.Create(ReportType.NalaOverview, "NALA_OVERVIEW_INSTITUTIONAL", "Resumen NALA", "1.0", ExportScope.Institutional, 5, 90);
        yield return ReportDefinition.Create(ReportType.NalaOverview, "NALA_OVERVIEW", "Resumen NALA", "1.0", ExportScope.Nala, 5, 90);
        yield return ReportDefinition.Create(ReportType.Recovery, "PUBLIC_RECOVERY_SUMMARY", "Impacto público de recuperación", "1.0", ExportScope.Public, 5, 30);
        yield return ReportDefinition.Create(ReportType.WelfareCases, "PUBLIC_WELFARE_SUMMARY", "Impacto público de bienestar", "1.0", ExportScope.Public, 5, 30);
    }
}
