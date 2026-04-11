using PawTrack.Domain.Pets;

namespace PawTrack.Domain.Fosters;

public sealed class FosterVolunteer
{
    private FosterVolunteer() { } // EF Core

    public Guid UserId { get; private set; }
    public string FullName { get; private set; } = string.Empty;
    public double HomeLat { get; private set; }
    public double HomeLng { get; private set; }
    public string AcceptedSpeciesCsv { get; private set; } = string.Empty;
    public string? SizePreference { get; private set; }
    public int MaxDays { get; private set; }
    public bool IsAvailable { get; private set; }
    public DateTimeOffset? AvailableUntil { get; private set; }
    public int TotalFostersCompleted { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public IReadOnlyList<PetSpecies> AcceptedSpecies => ParseSpecies(AcceptedSpeciesCsv);

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
            AcceptedSpeciesCsv = BuildSpeciesCsv(acceptedSpecies),
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
        AcceptedSpeciesCsv = BuildSpeciesCsv(acceptedSpecies);
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

    private static string BuildSpeciesCsv(IReadOnlyList<PetSpecies> species) =>
        string.Join(',', species.Distinct().OrderBy(s => s).Select(s => s.ToString()));

    private static IReadOnlyList<PetSpecies> ParseSpecies(string csv)
    {
        if (string.IsNullOrWhiteSpace(csv))
            return [];

        return csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(s => Enum.TryParse<PetSpecies>(s, out var parsed) ? parsed : (PetSpecies?)null)
            .Where(s => s.HasValue)
            .Select(s => s!.Value)
            .Distinct()
            .ToList()
            .AsReadOnly();
    }
}
