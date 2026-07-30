namespace PawTrack.Domain.Pets.Events;

public sealed record PetReactivatedDomainEvent(Guid PetId, Guid OwnerId, string PetName);
