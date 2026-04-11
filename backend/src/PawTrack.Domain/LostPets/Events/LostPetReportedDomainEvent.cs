namespace PawTrack.Domain.LostPets.Events;

public sealed record LostPetReportedDomainEvent(
    Guid LostPetEventId,
    Guid PetId,
    Guid OwnerId);
