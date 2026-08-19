namespace PawTrack.Domain.Advertising;

/// <summary>Where in the app the billboard appears.</summary>
public enum BillboardPlacement
{
    /// <summary>Floating card overlay on the public map.</summary>
    Map = 0,
    /// <summary>Between pet cards on the owner dashboard.</summary>
    Dashboard = 1,
    /// <summary>Top banner in the store or clinic directory.</summary>
    Directory = 2,
    /// <summary>Above the lost-pets feed on the public map panel.</summary>
    Feed = 3,
}

public enum BillboardStatus { Draft, Active, Paused, Expired }

/// <summary>A time-boxed promotional card shown to users in a specific placement.</summary>
public sealed class Billboard
{
    private Billboard() { } // EF Core

    public Guid Id { get; private set; }
    /// <summary>FK to the advertiser user (Admin creates on behalf, or Store owner self-serves).</summary>
    public Guid OwnerId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string? Body { get; private set; }
    public string? ImageUrl { get; private set; }
    public string? CtaLabel { get; private set; }
    public string? CtaUrl { get; private set; }
    public BillboardPlacement Placement { get; private set; }
    public BillboardStatus Status { get; private set; }
    public DateTimeOffset StartsAt { get; private set; }
    public DateTimeOffset EndsAt { get; private set; }
    public int Priority { get; private set; } // higher = shown first when multiple active
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }

    public bool IsCurrentlyActive =>
        Status == BillboardStatus.Active &&
        DateTimeOffset.UtcNow >= StartsAt &&
        DateTimeOffset.UtcNow < EndsAt;

    // ── Factory ───────────────────────────────────────────────────────────────

    public static Billboard Create(
        Guid ownerId,
        string title,
        string? body,
        BillboardPlacement placement,
        DateTimeOffset startsAt,
        DateTimeOffset endsAt,
        string? ctaLabel = null,
        string? ctaUrl = null,
        int priority = 0)
    {
        if (endsAt <= startsAt) throw new ArgumentException("EndsAt must be after StartsAt.");
        return new Billboard
        {
            Id = Guid.CreateVersion7(),
            OwnerId = ownerId,
            Title = title.Trim(),
            Body = body?.Trim(),
            Placement = placement,
            Status = BillboardStatus.Draft,
            StartsAt = startsAt,
            EndsAt = endsAt,
            CtaLabel = ctaLabel?.Trim(),
            CtaUrl = ctaUrl?.Trim(),
            Priority = priority,
            CreatedAt = DateTimeOffset.UtcNow,
        };
    }

    // ── Behaviour ─────────────────────────────────────────────────────────────

    public void Activate() { Status = BillboardStatus.Active; UpdatedAt = DateTimeOffset.UtcNow; }
    public void Pause() { Status = BillboardStatus.Paused; UpdatedAt = DateTimeOffset.UtcNow; }
    public void Expire() { Status = BillboardStatus.Expired; UpdatedAt = DateTimeOffset.UtcNow; }
    public void SetImageUrl(string url) { ImageUrl = url; UpdatedAt = DateTimeOffset.UtcNow; }

    public void Update(
        string title, string? body, string? ctaLabel, string? ctaUrl,
        DateTimeOffset startsAt, DateTimeOffset endsAt, int priority)
    {
        if (endsAt <= startsAt) throw new ArgumentException("EndsAt must be after StartsAt.");
        Title = title.Trim();
        Body = body?.Trim();
        CtaLabel = ctaLabel?.Trim();
        CtaUrl = ctaUrl?.Trim();
        StartsAt = startsAt;
        EndsAt = endsAt;
        Priority = priority;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
