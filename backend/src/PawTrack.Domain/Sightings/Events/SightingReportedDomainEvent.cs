namespace PawTrack.Domain.Sightings.Events;

/// <summary>Raised when an anonymous sighting of a lost pet is reported.</summary>
public sealed record SightingReportedDomainEvent(
    Guid SightingId,
    Guid PetId,
    Guid? LostPetEventId);
