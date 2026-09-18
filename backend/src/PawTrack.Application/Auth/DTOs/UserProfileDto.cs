namespace PawTrack.Application.Auth.DTOs;

public sealed record UserProfileDto(
    string Id,
    string Email,
    string Name,
    bool IsEmailVerified,
    bool IsAdmin,
    DateTimeOffset CreatedAt,
    bool IsAdultConfirmed,
    bool HasHealthDataConsent)
{
    public static UserProfileDto FromDomain(PawTrack.Domain.Auth.User user) => new(
        user.Id.ToString(),
        user.Email,
        user.Name,
        user.IsEmailVerified,
        user.Role is PawTrack.Domain.Auth.UserRole.Admin or PawTrack.Domain.Auth.UserRole.SuperAdmin,
        user.CreatedAt,
        user.IsAdultConfirmed,
        user.HasHealthDataConsent);
}
