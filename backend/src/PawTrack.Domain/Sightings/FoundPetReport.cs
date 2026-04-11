using PawTrack.Domain.Pets;

namespace PawTrack.Domain.Sightings;

/// <summary>
/// An anonymous report from a person who found a stray pet and wants to help reunite it.
/// ContactName / ContactPhone are PII — surfaced only to the matched pet owner.
/// </summary>
public sealed class FoundPetReport
{
    private FoundPetReport() { } // EF Core

    public Guid Id { get; private set; }
    public PetSpecies FoundSpecies { get; private set; }
    public string? BreedEstimate { get; private set; }
    public string? ColorDescription { get; private set; }
    public string? SizeEstimate { get; private set; }
    public double FoundLat { get; private set; }
    public double FoundLng { get; private set; }
    public string? PhotoUrl { get; private set; }

    /// <summary>Name of the person who found the pet. PII — restricted to matched pet owner.</summary>
    public string ContactName { get; private set; } = string.Empty;

    /// <summary>Phone number of the finder. PII — restricted to matched pet owner.</summary>
    public string ContactPhone { get; private set; } = string.Empty;

    public string? Note { get; private set; }
    public FoundPetStatus Status { get; private set; }

    /// <summary>Best-match LostPetEvent at the time of reporting, if score exceeded threshold.</summary>
    public Guid? MatchedLostPetEventId { get; private set; }

    /// <summary>Match confidence percentage (0–100).</summary>
    public int? MatchScore { get; private set; }

    public DateTimeOffset ReportedAt { get; private set; }

    private readonly List<object> _domainEvents = [];
    public IReadOnlyList<object> DomainEvents => _domainEvents.AsReadOnly();
    public void ClearDomainEvents() => _domainEvents.Clear();

    public static FoundPetReport Create(
        PetSpecies foundSpecies,
        string? breedEstimate,
        string? colorDescription,
        string? sizeEstimate,
        double foundLat,
        double foundLng,
        string contactName,
        string contactPhone,
        string? note)
    {
        return new FoundPetReport
        {
            Id = Guid.CreateVersion7(),
            FoundSpecies = foundSpecies,
            BreedEstimate = string.IsNullOrWhiteSpace(breedEstimate) ? null : breedEstimate.Trim(),
            ColorDescription = string.IsNullOrWhiteSpace(colorDescription) ? null : colorDescription.Trim(),
            SizeEstimate = string.IsNullOrWhiteSpace(sizeEstimate) ? null : sizeEstimate.Trim(),
            FoundLat = foundLat,
            FoundLng = foundLng,
            PhotoUrl = null,
            ContactName = contactName.Trim(),
            ContactPhone = contactPhone.Trim(),
            Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim(),
            Status = FoundPetStatus.Open,
            MatchedLostPetEventId = null,
            MatchScore = null,
            ReportedAt = DateTimeOffset.UtcNow,
        };
    }

    public void SetPhoto(string photoUrl)
    {
        PhotoUrl = photoUrl;
    }

    public void Match(Guid lostPetEventId, int scorePercent)
    {
        MatchedLostPetEventId = lostPetEventId;
        MatchScore = scorePercent;
        Status = FoundPetStatus.Matched;
    }

    public void Close()
    {
        Status = FoundPetStatus.Closed;
    }
}
