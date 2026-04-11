namespace PawTrack.Domain.Auth.Events;

public sealed record UserRegisteredDomainEvent(
    Guid UserId,
    string Email,
    string Name,
    string VerificationToken);
