namespace PawTrack.Domain.SearchCoordination;

public sealed class SearchLocationSharingSession
{
    private SearchLocationSharingSession() { }

    public Guid Id { get; private set; }
    public Guid LostEventId { get; private set; }
    public Guid UserId { get; private set; }
    public string ConnectionId { get; private set; } = string.Empty;
    public bool IsPrecise { get; private set; }
    public DateTimeOffset StartedAt { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset? StoppedAt { get; private set; }
    public DateTimeOffset? ExpiredAt { get; private set; }
    public bool IsActive => StoppedAt is null && ExpiredAt is null;

    public static SearchLocationSharingSession Start(
        Guid lostEventId,
        Guid userId,
        string connectionId,
        bool isPrecise,
        DateTimeOffset startedAt,
        TimeSpan lifetime) => new()
        {
            Id = Guid.CreateVersion7(),
            LostEventId = lostEventId,
            UserId = userId,
            ConnectionId = connectionId.Trim(),
            IsPrecise = isPrecise,
            StartedAt = startedAt,
            ExpiresAt = startedAt.Add(lifetime),
        };

    public void Stop(DateTimeOffset stoppedAt)
    {
        if (IsActive) StoppedAt = stoppedAt;
    }

    public void Expire(DateTimeOffset expiredAt)
    {
        if (IsActive) ExpiredAt = expiredAt;
    }
}
