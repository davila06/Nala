namespace PawTrack.Application.Auth.DTOs;

public sealed record AuthTokenDto(
    string AccessToken,
    string RefreshToken,
    int ExpiresIn,
    UserProfileDto User);
