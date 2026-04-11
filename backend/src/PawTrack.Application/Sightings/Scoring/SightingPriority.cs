namespace PawTrack.Application.Sightings.Scoring;

public sealed record SightingPriority(
    int Score,
    SightingPriorityBadge Badge,
    string RecommendedAction);