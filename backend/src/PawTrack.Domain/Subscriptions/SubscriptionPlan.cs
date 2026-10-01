namespace PawTrack.Domain.Subscriptions;

public sealed class SubscriptionPlan
{
    private SubscriptionPlan() { }

    public Guid Id { get; private set; }
    public SubscriptionTier Tier { get; private set; }
    public string DisplayName { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public decimal? MonthlyPriceCrc { get; private set; }
    public decimal? AnnualPriceCrc { get; private set; }
    public bool IsActive { get; private set; }
    public string? CommercialApprovalReference { get; private set; }
    public Guid? CommercialApprovedByUserId { get; private set; }
    public DateTimeOffset? CommercialApprovedAt { get; private set; }
    public bool IsCommerciallyApproved => CommercialApprovalReference is not null
        && CommercialApprovedByUserId.HasValue
        && CommercialApprovedAt.HasValue;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public Guid Version { get; private set; }

    public static SubscriptionPlan Create(
        SubscriptionTier tier,
        string displayName,
        string description,
        decimal? monthlyPriceCrc,
        decimal? annualPriceCrc)
    {
        Validate(tier, displayName, description, monthlyPriceCrc, annualPriceCrc);
        var now = DateTimeOffset.UtcNow;
        return new SubscriptionPlan
        {
            Id = Guid.CreateVersion7(),
            Tier = tier,
            DisplayName = displayName.Trim(),
            Description = description.Trim(),
            MonthlyPriceCrc = monthlyPriceCrc,
            AnnualPriceCrc = annualPriceCrc,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now,
            Version = Guid.NewGuid(),
        };
    }

    public void Update(
        string displayName,
        string description,
        decimal? monthlyPriceCrc,
        decimal? annualPriceCrc)
    {
        Validate(Tier, displayName, description, monthlyPriceCrc, annualPriceCrc);
        DisplayName = displayName.Trim();
        Description = description.Trim();
        MonthlyPriceCrc = monthlyPriceCrc;
        AnnualPriceCrc = annualPriceCrc;
        RevokeCommercialApproval();
        UpdatedAt = DateTimeOffset.UtcNow;
        Version = Guid.NewGuid();
    }

    public void ApproveForCommercialPublication(Guid approvedByUserId, string approvalReference)
    {
        if (approvedByUserId == Guid.Empty)
            throw new ArgumentException("An approving administrator is required.", nameof(approvedByUserId));
        var normalizedReference = approvalReference?.Trim();
        if (normalizedReference is null
            || normalizedReference.Length is < 3 or > 200
            || !System.Text.RegularExpressions.Regex.IsMatch(
                normalizedReference,
                "^[A-Za-z0-9][A-Za-z0-9._:/-]*$",
                System.Text.RegularExpressions.RegexOptions.CultureInvariant))
            throw new ArgumentException(
                "An approval reference must be a 3-200 character identifier using letters, numbers, '.', '_', ':', '/', or '-'.",
                nameof(approvalReference));
        if (!IsActive)
            throw new InvalidOperationException("An inactive plan cannot be approved for publication.");

        CommercialApprovalReference = normalizedReference;
        CommercialApprovedByUserId = approvedByUserId;
        CommercialApprovedAt = DateTimeOffset.UtcNow;
        UpdatedAt = CommercialApprovedAt.Value;
        Version = Guid.NewGuid();
    }

    public void RevokeCommercialPublicationApproval()
    {
        if (!IsCommerciallyApproved)
            throw new InvalidOperationException("Subscription plan has no commercial approval to revoke.");

        RevokeCommercialApproval();
        UpdatedAt = DateTimeOffset.UtcNow;
        Version = Guid.NewGuid();
    }

    public void Deactivate()
    {
        IsActive = false;
        RevokeCommercialApproval();
        UpdatedAt = DateTimeOffset.UtcNow;
        Version = Guid.NewGuid();
    }

    private void RevokeCommercialApproval()
    {
        CommercialApprovalReference = null;
        CommercialApprovedByUserId = null;
        CommercialApprovedAt = null;
    }

    private static void Validate(
        SubscriptionTier tier,
        string displayName,
        string description,
        decimal? monthlyPriceCrc,
        decimal? annualPriceCrc)
    {
        if (!SubscriptionPricing.IsPaidTier(tier))
            throw new ArgumentException("Only paid subscription tiers can be managed as plans.");
        if (string.IsNullOrWhiteSpace(displayName) || displayName.Trim().Length > 120)
            throw new ArgumentException("Display name is required and must not exceed 120 characters.");
        if (description is null || description.Trim().Length > 2000)
            throw new ArgumentException("Description must not exceed 2000 characters.");
        if (monthlyPriceCrc is null && annualPriceCrc is null)
            throw new ArgumentException("At least one billing price is required.");
        if (monthlyPriceCrc is <= 0 || annualPriceCrc is <= 0)
            throw new ArgumentException("Prices must be greater than zero.");
    }
}
