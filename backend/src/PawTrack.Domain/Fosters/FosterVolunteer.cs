using System.Text.Json;
using PawTrack.Domain.Pets;

namespace PawTrack.Domain.Fosters;

public sealed class FosterVolunteer
{
    private FosterVolunteer() { } // EF Core

    public Guid UserId { get; private set; }
    public string FullName { get; private set; } = string.Empty;
    public double HomeLat { get; private set; }
    public double HomeLng { get; private set; }
    /// <summary>Species stored as a JSON array — replaces the old CSV format.</summary>
    public string AcceptedSpeciesJson { get; private set; } = "[]";
    public string? SizePreference { get; private set; }
    public int MaxDays { get; private set; }
    public bool IsAvailable { get; private set; }
    public DateTimeOffset? AvailableUntil { get; private set; }
    public int TotalFostersCompleted { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public IReadOnlyList<PetSpecies> AcceptedSpecies => ParseSpecies(AcceptedSpeciesJson);

    public static FosterVolunteer Create(
        Guid userId,
        string fullName,
        double homeLat,
        double homeLng,
        IReadOnlyList<PetSpecies> acceptedSpecies,
        string? sizePreference,
        int maxDays,
        bool isAvailable,
        DateTimeOffset? availableUntil)
    {
        return new FosterVolunteer
        {
            UserId = userId,
            FullName = fullName.Trim(),
            HomeLat = homeLat,
            HomeLng = homeLng,
            AcceptedSpeciesJson = BuildSpeciesJson(acceptedSpecies),
            SizePreference = string.IsNullOrWhiteSpace(sizePreference) ? null : sizePreference.Trim(),
            MaxDays = maxDays,
            IsAvailable = isAvailable,
            AvailableUntil = availableUntil,
            TotalFostersCompleted = 0,
            UpdatedAt = DateTimeOffset.UtcNow,
        };
    }

    public void UpdateProfile(
        string fullName,
        double homeLat,
        double homeLng,
        IReadOnlyList<PetSpecies> acceptedSpecies,
        string? sizePreference,
        int maxDays,
        bool isAvailable,
        DateTimeOffset? availableUntil)
    {
        FullName = fullName.Trim();
        HomeLat = homeLat;
        HomeLng = homeLng;
        AcceptedSpeciesJson = BuildSpeciesJson(acceptedSpecies);
        SizePreference = string.IsNullOrWhiteSpace(sizePreference) ? null : sizePreference.Trim();
        MaxDays = maxDays;
        IsAvailable = isAvailable;
        AvailableUntil = availableUntil;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void MarkFosterCompleted()
    {
        TotalFostersCompleted++;
        IsAvailable = true;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    private static string BuildSpeciesJson(IReadOnlyList<PetSpecies> species)
    {
        var sorted = species.Distinct().OrderBy(s => s).Select(s => s.ToString()).ToList();
        return JsonSerializer.Serialize(sorted);
    }

    private static IReadOnlyList<PetSpecies> ParseSpecies(string json)
    {
        if (string.IsNullOrWhiteSpace(json) || json == "[]")
            return [];

        try
        {
            var strings = JsonSerializer.Deserialize<List<string>>(json) ?? [];
            return strings
                .Select(s => Enum.TryParse<PetSpecies>(s, out var parsed) ? parsed : (PetSpecies?)null)
                .Where(s => s.HasValue)
                .Select(s => s!.Value)
                .Distinct()
                .ToList()
                .AsReadOnly();
        }
        catch
        {
            // Backward-compat: parse legacy CSV format if JSON deserialization fails.
            return json.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(s => Enum.TryParse<PetSpecies>(s, out var parsed) ? parsed : (PetSpecies?)null)
                .Where(s => s.HasValue)
                .Select(s => s!.Value)
                .Distinct()
                .ToList()
                .AsReadOnly();
        }
    }
}
