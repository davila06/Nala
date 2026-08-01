namespace PawTrack.Domain.Sightings;

/// <summary>Monthly counter for per-user AI visual-search quota enforcement (free plan = 3/month).</summary>
public sealed class AiSearchUsage
{
    private AiSearchUsage() { }

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }

    /// <summary>YYYYMM integer, e.g. 202608 for August 2026.</summary>
    public int YearMonth { get; private set; }

    public int Count { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public static AiSearchUsage Create(Guid userId, int yearMonth) =>
        new() { Id = Guid.CreateVersion7(), UserId = userId, YearMonth = yearMonth, Count = 0, UpdatedAt = DateTimeOffset.UtcNow };

    public void Increment()
    {
        Count++;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
