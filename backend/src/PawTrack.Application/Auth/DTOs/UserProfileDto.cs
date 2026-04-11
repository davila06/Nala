namespace PawTrack.Application.Auth.DTOs;

public sealed record UserProfileDto(
    string Id,
    string Email,
    string Name,
    bool IsEmailVerified,
    bool IsAdmin)
{
    public static UserProfileDto FromDomain(PawTrack.Domain.Auth.User user) => new(
        user.Id.ToString(),
        user.Email,
        user.Name,
        user.IsEmailVerified,
        user.Role == PawTrack.Domain.Auth.UserRole.Admin);
}
