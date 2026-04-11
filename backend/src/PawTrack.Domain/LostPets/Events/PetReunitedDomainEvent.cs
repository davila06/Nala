namespace PawTrack.Domain.LostPets.Events;

public sealed record PetReunitedDomainEvent(
    Guid PetId,
    Guid OwnerId);
