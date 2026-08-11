namespace PawTrack.Domain.Medical;

/// <summary>Static breed weight references in kg. Used to render chart reference bands.</summary>
public static class BreedWeightReference
{
    public sealed record WeightRange(decimal MinKg, decimal MaxKg, string Label);

    // ── By breed (lowercase normalized key) ──────────────────────────────────

    private static readonly Dictionary<string, WeightRange> _breeds = new(StringComparer.OrdinalIgnoreCase)
    {
        // Dogs
        ["chihuahua"] = new(1.5m, 3.0m, "Chihuahua"),
        ["yorkshire terrier"] = new(2.0m, 3.2m, "Yorkshire Terrier"),
        ["poodle toy"] = new(2.0m, 4.0m, "Poodle Toy"),
        ["poodle miniatura"] = new(5.0m, 9.0m, "Poodle Miniatura"),
        ["poodle standard"] = new(20m, 32m, "Poodle Standard"),
        ["maltés"] = new(1.4m, 3.0m, "Maltés"),
        ["shih tzu"] = new(4.0m, 7.3m, "Shih Tzu"),
        ["pomerania"] = new(1.4m, 3.2m, "Pomerania"),
        ["french bulldog"] = new(8.0m, 13m, "Bulldog Francés"),
        ["bulldog"] = new(18m, 25m, "Bulldog Inglés"),
        ["beagle"] = new(9.0m, 11m, "Beagle"),
        ["golden retriever"] = new(25m, 34m, "Golden Retriever"),
        ["labrador retriever"] = new(25m, 36m, "Labrador Retriever"),
        ["labrador"] = new(25m, 36m, "Labrador"),
        ["german shepherd"] = new(22m, 40m, "Pastor Alemán"),
        ["pastor alemán"] = new(22m, 40m, "Pastor Alemán"),
        ["rottweiler"] = new(35m, 60m, "Rottweiler"),
        ["doberman"] = new(27m, 45m, "Doberman"),
        ["boxer"] = new(25m, 35m, "Boxer"),
        ["husky"] = new(16m, 27m, "Husky Siberiano"),
        ["dachshund"] = new(3.0m, 5.0m, "Dachshund"),
        ["salchicha"] = new(3.0m, 5.0m, "Salchicha"),
        ["schnauzer miniatura"] = new(5.0m, 9.0m, "Schnauzer Miniatura"),
        ["cocker spaniel"] = new(7.0m, 14m, "Cocker Spaniel"),
        ["border collie"] = new(12m, 20m, "Border Collie"),
        ["australian shepherd"] = new(16m, 32m, "Pastor Australiano"),
        ["bichon frise"] = new(3.0m, 5.5m, "Bichón Frisé"),
        ["pitbull"] = new(14m, 27m, "Pitbull"),
        ["american bully"] = new(20m, 40m, "American Bully"),
        ["great dane"] = new(45m, 90m, "Gran Danés"),
        // Cats
        ["doméstico"] = new(3.5m, 5.5m, "Gato Doméstico"),
        ["siamese"] = new(3.0m, 4.5m, "Siamés"),
        ["persian"] = new(3.0m, 5.5m, "Persa"),
        ["maine coon"] = new(4.0m, 8.0m, "Maine Coon"),
        ["ragdoll"] = new(4.5m, 9.0m, "Ragdoll"),
        ["bengal"] = new(3.5m, 7.0m, "Bengalí"),
        ["scottish fold"] = new(2.7m, 6.0m, "Scottish Fold"),
        ["british shorthair"] = new(4.0m, 7.7m, "British Shorthair"),
    };

    // ── Species fallback ─────────────────────────────────────────────────────

    private static readonly Dictionary<string, WeightRange> _speciesFallback = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Dog"] = new(5m, 35m, "Perro (promedio)"),
        ["Cat"] = new(3m, 6m, "Gato (promedio)"),
        ["Rabbit"] = new(1m, 3m, "Conejo"),
        ["Bird"] = new(0.02m, 1m, "Ave"),
    };

    public static WeightRange? GetByBreed(string? breed)
    {
        if (string.IsNullOrWhiteSpace(breed)) return null;
        _breeds.TryGetValue(breed.Trim(), out var range);
        return range;
    }

    public static WeightRange? GetBySpecies(string species) =>
        _speciesFallback.TryGetValue(species, out var r) ? r : null;

    public static WeightRange? Resolve(string? breed, string species) =>
        GetByBreed(breed) ?? GetBySpecies(species);
}
