namespace PawTrack.Domain.Promotions;

/// <summary>Immutable record of one user redeeming one promotion code.</summary>
public sealed class PromotionCodeRedemption
{
    private PromotionCodeRedemption() { } // EF Core

    public Guid Id { get; private set; }
    public Guid PromotionCodeId { get; private set; }
    public Guid UserId { get; private set; }
    public Guid SubscriptionId { get; private set; }
    public DateTimeOffset RedeemedAt { get; private set; }

    public static PromotionCodeRedemption Create(
        Guid promotionCodeId, Guid userId, Guid subscriptionId) => new()
        {
            Id = Guid.CreateVersion7(),
            PromotionCodeId = promotionCodeId,
            UserId = userId,
            SubscriptionId = subscriptionId,
            RedeemedAt = DateTimeOffset.UtcNow,
        };
}
