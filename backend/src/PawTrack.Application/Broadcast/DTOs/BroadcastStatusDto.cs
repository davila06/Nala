namespace PawTrack.Application.Broadcast.DTOs;

public sealed record BroadcastStatusDto(
    string LostPetEventId,
    IReadOnlyList<BroadcastAttemptDto> Attempts,
    int SentCount,
    int FailedCount,
    int SkippedCount,
    int TotalClicks);
