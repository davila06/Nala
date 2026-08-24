using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Medical;

namespace PawTrack.Infrastructure.Medical;

/// <summary>
/// Seeds the BreedReferences table once from the data that was previously hardcoded
/// in <see cref="BreedWeightReference"/> and <see cref="BreedActivityBenchmark"/>.
/// No-op if the table already has rows.
/// </summary>
public sealed class BreedReferenceSeedHostedService(
    IServiceScopeFactory scopeFactory,
    ILogger<BreedReferenceSeedHostedService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var repo = scope.ServiceProvider.GetRequiredService<IBreedReferenceRepository>();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        if (await repo.AnyAsync(ct))
            return;

        logger.LogInformation("BreedReferenceSeedHostedService: seeding breed reference data");
        await repo.AddRangeAsync(BuildSeedData(), ct);
        await uow.SaveChangesAsync(ct);
        logger.LogInformation("BreedReferenceSeedHostedService: seed complete");
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;

    private static IEnumerable<BreedReference> BuildSeedData()
    {
        // ── Dog weight + activity ─────────────────────────────────────────────
        static BreedReference Dog(string key, string display,
            decimal wMin, decimal wMax,
            int aMin, int aMax, int kMin, int kMax, string energy) =>
            BreedReference.Create(key, display, "Dog", wMin, wMax, display, aMin, aMax, kMin, kMax, energy);

        yield return Dog("chihuahua", "Chihuahua", 1.5m, 3.0m, 30, 60, 2, 4, "low");
        yield return Dog("yorkshire terrier", "Yorkshire Terrier", 2.0m, 3.2m, 30, 60, 2, 4, "medium");
        yield return Dog("poodle toy", "Poodle Toy", 2.0m, 4.0m, 45, 60, 3, 5, "medium");
        yield return Dog("poodle miniatura", "Poodle Miniatura", 5.0m, 9.0m, 45, 60, 3, 5, "medium");
        yield return Dog("poodle standard", "Poodle Standard", 20m, 32m, 60, 90, 5, 10, "medium");
        yield return Dog("maltés", "Maltés", 1.4m, 3.0m, 20, 40, 1, 3, "low");
        yield return Dog("shih tzu", "Shih Tzu", 4.0m, 7.3m, 20, 40, 1, 3, "low");
        yield return Dog("pomerania", "Pomerania", 1.4m, 3.2m, 30, 60, 2, 4, "low");
        yield return Dog("french bulldog", "Bulldog Francés", 8.0m, 13m, 20, 40, 1, 3, "low");
        yield return Dog("bulldog", "Bulldog Inglés", 18m, 25m, 20, 40, 1, 3, "low");
        yield return Dog("pug", "Pug", 6.0m, 9.0m, 20, 30, 1, 2, "low");
        yield return Dog("beagle", "Beagle", 9.0m, 11m, 60, 90, 4, 8, "medium");
        yield return Dog("golden retriever", "Golden Retriever", 25m, 34m, 60, 90, 5, 10, "medium");
        yield return Dog("labrador retriever", "Labrador Retriever", 25m, 36m, 60, 90, 5, 10, "medium");
        yield return Dog("labrador", "Labrador", 25m, 36m, 60, 90, 5, 10, "medium");
        yield return Dog("german shepherd", "Pastor Alemán", 22m, 40m, 60, 120, 5, 12, "medium");
        yield return Dog("pastor alemán", "Pastor Alemán", 22m, 40m, 60, 120, 5, 12, "medium");
        yield return Dog("rottweiler", "Rottweiler", 35m, 60m, 60, 90, 4, 8, "medium");
        yield return Dog("doberman", "Doberman", 27m, 45m, 60, 90, 6, 10, "medium");
        yield return Dog("boxer", "Boxer", 25m, 35m, 60, 90, 4, 8, "medium");
        yield return Dog("husky", "Husky Siberiano", 16m, 27m, 90, 150, 10, 20, "high");
        yield return Dog("dachshund", "Dachshund", 3.0m, 5.0m, 30, 60, 2, 4, "medium");
        yield return Dog("salchicha", "Salchicha", 3.0m, 5.0m, 30, 60, 2, 4, "medium");
        yield return Dog("schnauzer miniatura", "Schnauzer Miniatura", 5.0m, 9.0m, 45, 60, 3, 5, "medium");
        yield return Dog("cocker spaniel", "Cocker Spaniel", 7.0m, 14m, 45, 60, 3, 6, "medium");
        yield return Dog("border collie", "Border Collie", 12m, 20m, 90, 150, 8, 15, "high");
        yield return Dog("australian shepherd", "Pastor Australiano", 16m, 32m, 90, 150, 8, 15, "high");
        yield return Dog("bichon frise", "Bichón Frisé", 3.0m, 5.5m, 30, 45, 2, 4, "low");
        yield return Dog("pitbull", "Pitbull", 14m, 27m, 60, 90, 5, 8, "medium");
        yield return Dog("american bully", "American Bully", 20m, 40m, 45, 60, 3, 6, "medium");
        yield return Dog("great dane", "Gran Danés", 45m, 90m, 45, 60, 3, 6, "low");
        yield return Dog("jack russell", "Jack Russell Terrier", 6m, 8m, 60, 120, 6, 10, "high");
        yield return Dog("dalmatian", "Dálmata", 23m, 32m, 90, 120, 8, 15, "high");

        // ── Cat weight + activity ─────────────────────────────────────────────
        static BreedReference Cat(string key, string display,
            decimal wMin, decimal wMax,
            int aMin, int aMax, string energy) =>
            BreedReference.Create(key, display, "Cat", wMin, wMax, display, aMin, aMax, 0, 1, energy);

        yield return Cat("doméstico", "Gato Doméstico", 3.5m, 5.5m, 15, 30, "low");
        yield return Cat("siamese", "Siamés", 3.0m, 4.5m, 20, 40, "medium");
        yield return Cat("persian", "Persa", 3.0m, 5.5m, 15, 25, "low");
        yield return Cat("maine coon", "Maine Coon", 4.0m, 8.0m, 20, 40, "medium");
        yield return Cat("ragdoll", "Ragdoll", 4.5m, 9.0m, 15, 30, "low");
        yield return Cat("bengal", "Bengalí", 3.5m, 7.0m, 30, 60, "high");
        yield return Cat("scottish fold", "Scottish Fold", 2.7m, 6.0m, 15, 30, "low");
        yield return Cat("british shorthair", "British Shorthair", 4.0m, 7.7m, 15, 30, "low");
        yield return Cat("domestic shorthair", "Gato Doméstico Común", 3.5m, 5.5m, 15, 30, "low");

        // ── Species fallbacks ─────────────────────────────────────────────────
        static BreedReference Fallback(string species, string display,
            decimal wMin, decimal wMax,
            int aMin, int aMax, int kMin, int kMax, string energy) =>
            BreedReference.Create(species.ToLower(), display, species, wMin, wMax, display, aMin, aMax, kMin, kMax, energy, isSpeciesFallback: true);

        yield return Fallback("Dog", "Perro (promedio)", 5m, 35m, 45, 90, 3, 8, "medium");
        yield return Fallback("Cat", "Gato (promedio)", 3m, 6m, 15, 30, 0, 1, "low");
        yield return Fallback("Rabbit", "Conejo", 1m, 3m, 15, 30, 0, 1, "medium");
        yield return Fallback("Bird", "Ave", 0.02m, 1m, 10, 20, 0, 0, "low");
        yield return Fallback("Other", "Otro (promedio)", 1m, 10m, 30, 60, 1, 4, "medium");
    }
}
