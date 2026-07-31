namespace PawTrack.Domain.Municipalities;

public sealed class CapturedAnimal
{
    private CapturedAnimal() { } // EF Core

    public Guid Id { get; private set; }
    public string Canton { get; private set; } = string.Empty;
    public string Species { get; private set; } = string.Empty;
    public string? Breed { get; private set; }
    public string Color { get; private set; } = string.Empty;
    public string? EstimatedAge { get; private set; }
    public string? PhotoUrl { get; private set; }
    public string? Notes { get; private set; }
    public string? CollarChipNumber { get; private set; }
    /// <summary>If the animal matched a PawTrack pet via QR or chip, store the pet ID.</summary>
    public Guid? MatchedPetId { get; private set; }
    public CapturedAnimalStatus Status { get; private set; }
    public DateTimeOffset CapturedAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public Guid RecordedByUserId { get; private set; }

    // ── Factory ─────────────────────────────────────────────────────────────

    public static CapturedAnimal Record(
        Guid recordedByUserId,
        string canton,
        string species,
        string color,
        string? breed = null,
        string? estimatedAge = null,
        string? notes = null,
        string? collarChipNumber = null,
        DateTimeOffset? capturedAt = null)
    {
        return new CapturedAnimal
        {
            Id = Guid.CreateVersion7(),
            RecordedByUserId = recordedByUserId,
            Canton = canton,
            Species = species,
            Color = color,
            Breed = breed,
            EstimatedAge = estimatedAge,
            Notes = notes,
            CollarChipNumber = collarChipNumber,
            Status = CapturedAnimalStatus.Received,
            CapturedAt = capturedAt ?? DateTimeOffset.UtcNow,
            CreatedAt = DateTimeOffset.UtcNow,
        };
    }

    // ── Behaviour ───────────────────────────────────────────────────────────

    public void SetPhotoUrl(string url) => PhotoUrl = url;

    public void LinkToPet(Guid petId) => MatchedPetId = petId;

    public void UpdateStatus(CapturedAnimalStatus status) => Status = status;

    public void UpdateDetails(string? notes, string? collarChipNumber)
    {
        Notes = notes;
        CollarChipNumber = collarChipNumber;
    }
}
