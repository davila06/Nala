using PawTrack.Domain.Pets;

namespace PawTrack.Domain.Adoptions;

public enum PetSize { XSmall, Small, Medium, Large, XLarge }

public enum AdoptionStatus { Available, InProcess, Adopted, Paused, Removed }

/// <summary>Approximate age bucket — maps to: &lt;1y, 1-3y, 3-8y, 8y+.</summary>
public enum AgeCategory { Puppy, Young, Adult, Senior }

public sealed class AdoptablePet
{
    private AdoptablePet() { } // EF Core
    private readonly List<string> _photoUrls = [];

    public Guid Id { get; private set; }
    /// <summary>FK to AllyProfile.UserId — the shelter that published this animal.</summary>
    public Guid OrganizationUserId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public PetSpecies Species { get; private set; }
    public string? Breed { get; private set; }
    public PetSize Size { get; private set; }
    public AgeCategory AgeCategory { get; private set; }
    public int? AgeMonthsApprox { get; private set; }
    public string Story { get; private set; } = string.Empty;
    public string? Requirements { get; private set; }
    public string? MedicalNotes { get; private set; }
    public bool IsVaccinated { get; private set; }
    public bool IsSterilized { get; private set; }
    public bool IsMicrochipped { get; private set; }
    public bool OkWithKids { get; private set; }
    public bool OkWithDogs { get; private set; }
    public bool OkWithCats { get; private set; }
    public bool NeedsYard { get; private set; }
    /// <summary>Reference coordinate — NOT the shelter's exact address.</summary>
    public double RefLat { get; private set; }
    public double RefLng { get; private set; }
    public string? RefLabel { get; private set; }
    public AdoptionStatus Status { get; private set; }
    public DateTimeOffset PublishedAt { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }
    public DateTimeOffset? AdoptedAt { get; private set; }

    public IReadOnlyList<string> PhotoUrls => _photoUrls.AsReadOnly();

    // ── Factory ───────────────────────────────────────────────────────────────

    public static AdoptablePet Create(
        Guid organizationUserId,
        string name,
        PetSpecies species,
        PetSize size,
        AgeCategory ageCategory,
        string story,
        double refLat,
        double refLng,
        string? refLabel,
        string? breed = null,
        int? ageMonthsApprox = null,
        string? requirements = null,
        string? medicalNotes = null,
        bool isVaccinated = false,
        bool isSterilized = false,
        bool isMicrochipped = false,
        bool okWithKids = false,
        bool okWithDogs = false,
        bool okWithCats = false,
        bool needsYard = false) => new()
        {
            Id = Guid.CreateVersion7(),
            OrganizationUserId = organizationUserId,
            Name = name.Trim(),
            Species = species,
            Breed = breed?.Trim(),
            Size = size,
            AgeCategory = ageCategory,
            AgeMonthsApprox = ageMonthsApprox,
            Story = story.Trim(),
            Requirements = requirements?.Trim(),
            MedicalNotes = medicalNotes?.Trim(),
            IsVaccinated = isVaccinated,
            IsSterilized = isSterilized,
            IsMicrochipped = isMicrochipped,
            OkWithKids = okWithKids,
            OkWithDogs = okWithDogs,
            OkWithCats = okWithCats,
            NeedsYard = needsYard,
            RefLat = refLat,
            RefLng = refLng,
            RefLabel = refLabel?.Trim(),
            Status = AdoptionStatus.Available,
            PublishedAt = DateTimeOffset.UtcNow,
        };

    // ── Behaviour ─────────────────────────────────────────────────────────────

    public void AddPhoto(string url)
    {
        if (_photoUrls.Count >= 5)
            throw new InvalidOperationException("Maximum 5 photos per animal.");
        _photoUrls.Add(url);
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void RemovePhoto(string url)
    {
        _photoUrls.Remove(url);
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void MarkInProcess() { Status = AdoptionStatus.InProcess; UpdatedAt = DateTimeOffset.UtcNow; }
    public void MarkAdopted()   { Status = AdoptionStatus.Adopted; AdoptedAt = DateTimeOffset.UtcNow; }
    public void Pause()         { Status = AdoptionStatus.Paused; UpdatedAt = DateTimeOffset.UtcNow; }
    public void Republish()     { Status = AdoptionStatus.Available; UpdatedAt = DateTimeOffset.UtcNow; }
    public void Remove()        { Status = AdoptionStatus.Removed; UpdatedAt = DateTimeOffset.UtcNow; }

    public void UpdateDetails(
        string name,
        string story,
        string? requirements,
        string? medicalNotes,
        bool isVaccinated,
        bool isSterilized,
        bool isMicrochipped,
        bool okWithKids,
        bool okWithDogs,
        bool okWithCats,
        bool needsYard)
    {
        Name = name.Trim();
        Story = story.Trim();
        Requirements = requirements?.Trim();
        MedicalNotes = medicalNotes?.Trim();
        IsVaccinated = isVaccinated;
        IsSterilized = isSterilized;
        IsMicrochipped = isMicrochipped;
        OkWithKids = okWithKids;
        OkWithDogs = okWithDogs;
        OkWithCats = okWithCats;
        NeedsYard = needsYard;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
