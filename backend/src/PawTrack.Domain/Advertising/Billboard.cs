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
    PublicPetProfile = 4,
    ScanHistory = 5,
    CaseRoom = 6,
    ClinicDirectory = 7,
    ClinicProfile = 8,
    ServiceProviderDirectory = 9,
    ServiceProviderProfile = 10,
    AdoptionDirectory = 11,
    AdoptionFair = 12,
    PetRegistration = 13,
    CollarActivation = 14,
}

public enum BillboardStatus { Draft, Active, Paused, Expired }

public enum BillboardCampaignStatus { Draft, PendingReview, Approved, Rejected }

public enum BillboardCategory
{
    EmergencyVeterinary,
    RecoveryService,
    GpsAndIdentification,
    PetInsurance,
    FoodAndNutrition,
    PetCare,
    Training,
    Boarding,
    AdoptionSupport,
}

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
    public string AdvertiserName { get; private set; } = string.Empty;
    public BillboardCategory Category { get; private set; }
    public string? TargetCanton { get; private set; }
    public string? ContractReference { get; private set; }
    public decimal BudgetCrc { get; private set; }
    public int FrequencyCapPerDay { get; private set; }
    public bool IsCategoryExclusive { get; private set; }
    public bool IsVip { get; private set; }
    public BillboardCampaignStatus CampaignStatus { get; private set; }
    public Guid? ReviewedByUserId { get; private set; }
    public DateTimeOffset? ReviewedAt { get; private set; }
    public string? ReviewNote { get; private set; }
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
        int priority = 0,
        string advertiserName = "Pendiente de anunciante",
        BillboardCategory category = BillboardCategory.PetCare,
        string? targetCanton = null,
        string? contractReference = null,
        decimal budgetCrc = 0,
        int frequencyCapPerDay = 1,
        bool isCategoryExclusive = false,
        bool isVip = false)
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
            Priority = isVip ? Math.Max(priority, 90) : priority,
            AdvertiserName = advertiserName.Trim(),
            Category = category,
            TargetCanton = targetCanton?.Trim(),
            ContractReference = contractReference?.Trim(),
            BudgetCrc = budgetCrc,
            FrequencyCapPerDay = Math.Clamp(frequencyCapPerDay, 1, 10),
            IsCategoryExclusive = isCategoryExclusive,
            IsVip = isVip,
            CampaignStatus = BillboardCampaignStatus.Draft,
            CreatedAt = DateTimeOffset.UtcNow,
        };
    }

    // ── Behaviour ─────────────────────────────────────────────────────────────

    public bool SubmitForReview()
    {
        if (string.IsNullOrWhiteSpace(AdvertiserName) || string.IsNullOrWhiteSpace(ImageUrl)) return false;
        CampaignStatus = BillboardCampaignStatus.PendingReview;
        UpdatedAt = DateTimeOffset.UtcNow;
        return true;
    }

    public bool Approve(Guid reviewerUserId, string? note = null)
    {
        if (reviewerUserId == OwnerId || !IsCategoryAllowed(Placement, Category))
        {
            CampaignStatus = BillboardCampaignStatus.Rejected;
            ReviewNote = reviewerUserId == OwnerId
                ? "La aprobación requiere un segundo operador."
                : "La categoría no es admisible para este placement.";
            ReviewedByUserId = reviewerUserId;
            ReviewedAt = DateTimeOffset.UtcNow;
            UpdatedAt = ReviewedAt;
            return false;
        }

        CampaignStatus = BillboardCampaignStatus.Approved;
        ReviewedByUserId = reviewerUserId;
        ReviewedAt = DateTimeOffset.UtcNow;
        ReviewNote = note?.Trim();
        UpdatedAt = ReviewedAt;
        return true;
    }

    public void Reject(Guid reviewerUserId, string reason)
    {
        CampaignStatus = BillboardCampaignStatus.Rejected;
        ReviewedByUserId = reviewerUserId;
        ReviewedAt = DateTimeOffset.UtcNow;
        ReviewNote = reason.Trim();
        UpdatedAt = ReviewedAt;
    }

    public void Activate()
    {
        if (CampaignStatus != BillboardCampaignStatus.Approved || string.IsNullOrWhiteSpace(ImageUrl)) return;
        Status = BillboardStatus.Active;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
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

    public void UpdateCampaign(
        string title, string? body, string? ctaLabel, string? ctaUrl,
        DateTimeOffset startsAt, DateTimeOffset endsAt, int priority,
        string advertiserName, BillboardCategory category, string? targetCanton,
        string? contractReference, decimal budgetCrc, int frequencyCapPerDay,
        bool isCategoryExclusive, bool isVip = false)
    {
        Update(title, body, ctaLabel, ctaUrl, startsAt, endsAt, priority);
        AdvertiserName = advertiserName.Trim();
        Category = category;
        TargetCanton = targetCanton?.Trim();
        ContractReference = contractReference?.Trim();
        BudgetCrc = budgetCrc;
        FrequencyCapPerDay = Math.Clamp(frequencyCapPerDay, 1, 10);
        IsCategoryExclusive = isCategoryExclusive;
        IsVip = isVip;
        Priority = isVip ? Math.Max(priority, 90) : priority;
        CampaignStatus = BillboardCampaignStatus.Draft;
        Status = BillboardStatus.Draft;
        ReviewedByUserId = null;
        ReviewedAt = null;
        ReviewNote = null;
    }

    public static bool IsCategoryAllowed(BillboardPlacement placement, BillboardCategory category) =>
        placement is not (BillboardPlacement.Feed or BillboardPlacement.CaseRoom) ||
        category is BillboardCategory.EmergencyVeterinary or BillboardCategory.RecoveryService or
            BillboardCategory.GpsAndIdentification or BillboardCategory.PetInsurance;
}
