namespace PawTrack.Domain.Medical;

/// <summary>Static breed activity benchmarks. Used for the activity tab's progress bar and streak comparisons.</summary>
public static class BreedActivityBenchmark
{
    public sealed record ActivityRange(int DailyMinutesMin, int DailyMinutesMax, int DailyKmMin, int DailyKmMax, string EnergyLevel);

    private static readonly Dictionary<string, ActivityRange> _breeds = new(StringComparer.OrdinalIgnoreCase)
    {
        // Dogs — high energy
        ["border collie"] = new(90, 150, 8, 15, "high"),
        ["australian shepherd"] = new(90, 150, 8, 15, "high"),
        ["husky"] = new(90, 150, 10, 20, "high"),
        ["jack russell"] = new(60, 120, 6, 10, "high"),
        ["dalmatian"] = new(90, 120, 8, 15, "high"),
        // Dogs — medium energy
        ["labrador retriever"] = new(60, 90, 5, 10, "medium"),
        ["labrador"] = new(60, 90, 5, 10, "medium"),
        ["golden retriever"] = new(60, 90, 5, 10, "medium"),
        ["german shepherd"] = new(60, 120, 5, 12, "medium"),
        ["pastor alemán"] = new(60, 120, 5, 12, "medium"),
        ["beagle"] = new(60, 90, 4, 8, "medium"),
        ["boxer"] = new(60, 90, 4, 8, "medium"),
        ["doberman"] = new(60, 90, 6, 10, "medium"),
        ["cocker spaniel"] = new(45, 60, 3, 6, "medium"),
        // Dogs — low energy
        ["french bulldog"] = new(20, 40, 1, 3, "low"),
        ["bulldog"] = new(20, 40, 1, 3, "low"),
        ["pug"] = new(20, 30, 1, 2, "low"),
        ["shih tzu"] = new(20, 40, 1, 3, "low"),
        ["chihuahua"] = new(30, 60, 2, 4, "low"),
        // Cats
        ["domestic shorthair"] = new(15, 30, 0, 1, "low"),
        ["maine coon"] = new(20, 40, 0, 1, "medium"),
        ["bengal"] = new(30, 60, 1, 2, "high"),
    };

    private static readonly Dictionary<string, ActivityRange> _speciesFallback = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Dog"] = new(45, 90, 3, 8, "medium"),
        ["Cat"] = new(15, 30, 0, 1, "low"),
        ["Rabbit"] = new(15, 30, 0, 1, "medium"),
        ["Bird"] = new(10, 20, 0, 0, "low"),
        ["Other"] = new(30, 60, 1, 4, "medium"),
    };

    public static ActivityRange? GetByBreed(string? breed) =>
        breed is not null && _breeds.TryGetValue(breed, out var r) ? r : null;

    public static ActivityRange? GetBySpecies(string species) =>
        _speciesFallback.TryGetValue(species, out var r) ? r : null;

    public static ActivityRange? Resolve(string? breed, string species) =>
        GetByBreed(breed) ?? GetBySpecies(species);
}
