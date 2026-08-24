namespace PawTrack.Domain.Medical;

/// <summary>
/// DB-backed breed reference data (weight ranges and activity benchmarks).
/// Replaces the hardcoded static dictionaries in <see cref="BreedWeightReference"/>
/// and <see cref="BreedActivityBenchmark"/>.
/// </summary>
public sealed class BreedReference
{
    private BreedReference() { } // EF Core

    public Guid Id { get; private set; }
    /// <summary>Normalized lowercase key used for lookup (e.g. "golden retriever").</summary>
    public string BreedKey { get; private set; } = string.Empty;
    /// <summary>Display name shown to the user (e.g. "Golden Retriever").</summary>
    public string DisplayName { get; private set; } = string.Empty;
    /// <summary>Species this reference applies to (matches <see cref="Pets.PetSpecies"/> string).</summary>
    public string Species { get; private set; } = string.Empty;

    // ── Weight ───────────────────────────────────────────────────────────────
    public decimal? WeightMinKg { get; private set; }
    public decimal? WeightMaxKg { get; private set; }
    public string? WeightLabel { get; private set; }

    // ── Activity ─────────────────────────────────────────────────────────────
    public int? ActivityMinMinutes { get; private set; }
    public int? ActivityMaxMinutes { get; private set; }
    public int? ActivityMinKm { get; private set; }
    public int? ActivityMaxKm { get; private set; }
    public string? EnergyLevel { get; private set; }

    public bool IsSpeciesFallback { get; private set; }

    // ── Factory ───────────────────────────────────────────────────────────────

    public static BreedReference Create(
        string breedKey,
        string displayName,
        string species,
        decimal? weightMinKg = null,
        decimal? weightMaxKg = null,
        string? weightLabel = null,
        int? activityMinMinutes = null,
        int? activityMaxMinutes = null,
        int? activityMinKm = null,
        int? activityMaxKm = null,
        string? energyLevel = null,
        bool isSpeciesFallback = false) => new()
        {
            Id = Guid.CreateVersion7(),
            BreedKey = breedKey.Trim().ToLowerInvariant(),
            DisplayName = displayName.Trim(),
            Species = species.Trim(),
            WeightMinKg = weightMinKg,
            WeightMaxKg = weightMaxKg,
            WeightLabel = weightLabel?.Trim(),
            ActivityMinMinutes = activityMinMinutes,
            ActivityMaxMinutes = activityMaxMinutes,
            ActivityMinKm = activityMinKm,
            ActivityMaxKm = activityMaxKm,
            EnergyLevel = energyLevel?.Trim(),
            IsSpeciesFallback = isSpeciesFallback,
        };
}
