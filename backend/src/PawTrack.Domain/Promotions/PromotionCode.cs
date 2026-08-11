using System.Security.Cryptography;
using PawTrack.Domain.Subscriptions;

namespace PawTrack.Domain.Promotions;

/// <summary>
/// An admin-created promotion code redeemable by users.
/// Code format encodes its benefit so it is human-readable:
///   DES10XXX / DES15XXX  — 10% or 15% discount (X = CSPRNG suffix)
///   FREEPLXX / FREEFAXX  — free Plus or Familia tier (1 month default)
///   MES01XXX / MES03XXX / MES06XXX — free months (tier stored separately)
/// </summary>
public sealed class PromotionCode
{
    // Unambiguous charset — excludes I, L, O, 0, 1 (reused from ClinicMedicalAccessGrant)
    private const string Charset = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";

    private PromotionCode() { } // EF Core

    public Guid Id { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public PromotionType Type { get; private set; }

    /// <summary>10 or 15 for PercentageDiscount; null otherwise.</summary>
    public int? DiscountPercent { get; private set; }

    /// <summary>1, 3, or 6 for FreeMonths; 1 for FreeTier; null for partial discount.</summary>
    public int? FreeMonths { get; private set; }

    /// <summary>Required for FreeTier and FreeMonths; optional filter for PercentageDiscount.</summary>
    public SubscriptionTier? TargetTier { get; private set; }

    /// <summary>How many total redemptions this code allows across all users. -1 = unlimited.</summary>
    public int MaxRedemptions { get; private set; }

    /// <summary>Incremented atomically with optimistic concurrency on each redemption.</summary>
    public int RedeemedCount { get; private set; }

    public DateTimeOffset? ExpiresAt { get; private set; }
    public bool IsActive { get; private set; }
    public Guid CreatedByAdminId { get; private set; }
    public string? AdminNote { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    // ── Computed ─────────────────────────────────────────────────────────────

    public bool CanRedeem =>
        IsActive &&
        (ExpiresAt is null || ExpiresAt > DateTimeOffset.UtcNow) &&
        (MaxRedemptions == -1 || RedeemedCount < MaxRedemptions);

    /// <summary>True when this code grants a subscription without any SINPE payment.</summary>
    public bool IsFullyFree =>
        Type is PromotionType.FreeTier or PromotionType.FreeMonths ||
        DiscountPercent == 100;

    // ── Factories ─────────────────────────────────────────────────────────────

    public static PromotionCode CreateDiscount(
        int discountPercent,
        SubscriptionTier? targetTier,
        int maxRedemptions,
        DateTimeOffset? expiresAt,
        Guid adminId,
        string? adminNote)
    {
        if (discountPercent is not (10 or 15 or 100))
            throw new ArgumentException("Discount must be 10, 15, or 100.", nameof(discountPercent));

        var prefix = $"DES{discountPercent:00}";
        return Build(prefix, 3, PromotionType.PercentageDiscount,
            discountPercent, null, targetTier, maxRedemptions, expiresAt, adminId, adminNote);
    }

    public static PromotionCode CreateFreeTier(
        SubscriptionTier tier,
        int maxRedemptions,
        DateTimeOffset? expiresAt,
        Guid adminId,
        string? adminNote)
    {
        if (tier is not (SubscriptionTier.UserPlus or SubscriptionTier.UserFamilia))
            throw new ArgumentException("FreeTier only supports UserPlus or UserFamilia.", nameof(tier));

        var tierCode = tier == SubscriptionTier.UserPlus ? "PL" : "FA";
        var prefix = $"FREE{tierCode}";
        return Build(prefix, 2, PromotionType.FreeTier,
            null, 1, tier, maxRedemptions, expiresAt, adminId, adminNote);
    }

    public static PromotionCode CreateFreeMonths(
        int months,
        SubscriptionTier tier,
        int maxRedemptions,
        DateTimeOffset? expiresAt,
        Guid adminId,
        string? adminNote)
    {
        if (months is not (1 or 3 or 6))
            throw new ArgumentException("FreeMonths duration must be 1, 3, or 6.", nameof(months));
        if (tier is not (SubscriptionTier.UserPlus or SubscriptionTier.UserFamilia))
            throw new ArgumentException("FreeMonths only supports UserPlus or UserFamilia.", nameof(tier));

        var prefix = $"MES{months:00}";
        return Build(prefix, 3, PromotionType.FreeMonths,
            null, months, tier, maxRedemptions, expiresAt, adminId, adminNote);
    }

    // ── Behaviour ─────────────────────────────────────────────────────────────

    /// <summary>Increment redemption counter. Caller must verify CanRedeem first.</summary>
    public void IncrementRedeemed() => RedeemedCount++;

    public void Deactivate() => IsActive = false;
    public void Reactivate() => IsActive = true;

    public void UpdateLimits(int maxRedemptions, DateTimeOffset? expiresAt)
    {
        // Immutable type/benefit — only operational limits can change after first use
        MaxRedemptions = maxRedemptions;
        ExpiresAt = expiresAt;
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private static PromotionCode Build(
        string prefix,
        int suffixLength,
        PromotionType type,
        int? discountPercent,
        int? freeMonths,
        SubscriptionTier? targetTier,
        int maxRedemptions,
        DateTimeOffset? expiresAt,
        Guid adminId,
        string? adminNote) => new()
        {
            Id = Guid.CreateVersion7(),
            Code = prefix + GenerateSuffix(suffixLength),
            Type = type,
            DiscountPercent = discountPercent,
            FreeMonths = freeMonths,
            TargetTier = targetTier,
            MaxRedemptions = maxRedemptions,
            RedeemedCount = 0,
            ExpiresAt = expiresAt,
            IsActive = true,
            CreatedByAdminId = adminId,
            AdminNote = adminNote?.Trim(),
            CreatedAt = DateTimeOffset.UtcNow,
        };

    private static string GenerateSuffix(int length)
    {
        var bytes = new byte[length];
        RandomNumberGenerator.Fill(bytes);
        return new string(bytes.Select(b => Charset[b % Charset.Length]).ToArray());
    }
}
