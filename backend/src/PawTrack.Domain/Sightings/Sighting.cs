using PawTrack.Domain.Sightings.Events;

namespace PawTrack.Domain.Sightings;

/// <summary>
/// An anonymous geo-tagged sighting of a pet.
/// PII is stripped by the application layer before constructing this entity —
/// no reporter contact details are ever persisted.
/// </summary>
public sealed class Sighting
{
    private Sighting() { } // EF Core

    public Guid Id { get; private set; }
    public Guid PetId { get; private set; }
    public Guid? LostPetEventId { get; private set; }

    // PII ELIMINATED — reporter contact details are never stored.
    public double Lat { get; private set; }
    public double Lng { get; private set; }
    public string? PhotoUrl { get; private set; }

    /// <summary>Free-form note, pre-sanitised by PiiScrubber before persistence.</summary>
    public string? Note { get; private set; }

    public DateTimeOffset SightedAt { get; private set; }
    public DateTimeOffset ReportedAt { get; private set; }

    private readonly List<object> _domainEvents = [];
    public IReadOnlyList<object> DomainEvents => _domainEvents.AsReadOnly();
    public void ClearDomainEvents() => _domainEvents.Clear();

    public static Sighting Create(
        Guid petId,
        Guid? lostPetEventId,
        double lat,
        double lng,
        string? sanitisedNote,
        DateTimeOffset sightedAt)
    {
        var sighting = new Sighting
        {
            Id = Guid.CreateVersion7(),
            PetId = petId,
            LostPetEventId = lostPetEventId,
            Lat = lat,
            Lng = lng,
            PhotoUrl = null,
            Note = string.IsNullOrWhiteSpace(sanitisedNote) ? null : sanitisedNote.Trim(),
            SightedAt = sightedAt,
            ReportedAt = DateTimeOffset.UtcNow,
        };

        sighting._domainEvents.Add(
            new SightingReportedDomainEvent(sighting.Id, petId, lostPetEventId));

        return sighting;
    }

    public void SetPhoto(string photoUrl)
    {
        PhotoUrl = photoUrl;
    }
}
