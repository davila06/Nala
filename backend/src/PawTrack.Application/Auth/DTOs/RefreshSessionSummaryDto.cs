namespace PawTrack.Application.Auth.DTOs;

public sealed record RefreshSessionSummaryDto(
    Guid SessionId,
    DateTimeOffset StartedAt,
    DateTimeOffset LastActivityAt,
    DateTimeOffset ExpiresAt);
