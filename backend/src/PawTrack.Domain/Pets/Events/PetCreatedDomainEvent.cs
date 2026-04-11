namespace PawTrack.Domain.Pets.Events;

public sealed record PetCreatedDomainEvent(
    Guid PetId,
    Guid OwnerId,
    string PetName);
