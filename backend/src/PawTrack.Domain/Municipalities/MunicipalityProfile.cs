namespace PawTrack.Domain.Municipalities;

/// <summary>Subscription profile for a municipality user, controlling feature tier access.</summary>
public sealed class MunicipalityProfile
{
    private MunicipalityProfile() { } // EF Core

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }

    /// <summary>Primary canton this municipality manages.</summary>
    public string Canton { get; private set; } = string.Empty;

    /// <summary>Official organization name (e.g. "Municipalidad de San José").</summary>
    public string OrgName { get; private set; } = string.Empty;

    public MunicipalTier Tier { get; private set; }

    /// <summary>RedRegional: comma-separated list of additional cantons under the same contract.</summary>
    public string? AdditionalCantons { get; private set; }

    public bool IsActive { get; private set; }
    public DateTimeOffset SubscribedAt { get; private set; }
    public DateTimeOffset? ExpiresAt { get; private set; }

    // ── Factory ───────────────────────────────────────────────────────────────

    public static MunicipalityProfile Create(
        Guid userId, string canton, string orgName,
        MunicipalTier tier, DateTimeOffset? expiresAt = null)
    {
        return new MunicipalityProfile
        {
            Id = Guid.CreateVersion7(),
            UserId = userId,
            Canton = canton.Trim(),
            OrgName = orgName.Trim(),
            Tier = tier,
            IsActive = true,
            SubscribedAt = DateTimeOffset.UtcNow,
            ExpiresAt = expiresAt,
        };
    }

    // ── Behaviour ─────────────────────────────────────────────────────────────

    public void Upgrade(MunicipalTier newTier, DateTimeOffset? newExpiry = null)
    {
        Tier = newTier;
        if (newExpiry.HasValue) ExpiresAt = newExpiry;
    }

    public void SetAdditionalCantons(IEnumerable<string> cantons) =>
        AdditionalCantons = string.Join(",", cantons.Select(c => c.Trim()).Where(c => c.Length > 0));

    public void Deactivate() => IsActive = false;

    // ── Computed ──────────────────────────────────────────────────────────────

    public bool IsExpired => ExpiresAt.HasValue && ExpiresAt.Value < DateTimeOffset.UtcNow;

    public bool IsFullOrAbove => IsActive && !IsExpired && Tier >= MunicipalTier.Full;

    public bool IsRedRegional => IsActive && !IsExpired && Tier == MunicipalTier.RedRegional;

    public IReadOnlyList<string> AllCantons
    {
        get
        {
            var list = new List<string> { Canton };
            if (!string.IsNullOrWhiteSpace(AdditionalCantons))
                list.AddRange(AdditionalCantons.Split(',', StringSplitOptions.RemoveEmptyEntries));
            return list.AsReadOnly();
        }
    }
}
