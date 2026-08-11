using PawTrack.Domain.Promotions;

namespace PawTrack.Application.Promotions.Interfaces;

public interface IPromotionCodeRepository
{
    Task AddRangeAsync(IEnumerable<PromotionCode> codes, CancellationToken ct = default);
    /// <summary>Case-insensitive lookup. Returns tracking instance for update.</summary>
    Task<PromotionCode?> GetByCodeAsync(string code, CancellationToken ct = default);
    Task<IReadOnlyList<PromotionCode>> GetAllAsync(CancellationToken ct = default);
    void Update(PromotionCode code);

    Task<bool> HasUserRedeemedAsync(Guid userId, Guid promotionCodeId, CancellationToken ct = default);
    Task<bool> HasUserActivePromoSubscriptionAsync(Guid userId, CancellationToken ct = default);
    Task AddRedemptionAsync(PromotionCodeRedemption redemption, CancellationToken ct = default);
    Task<IReadOnlyList<PromotionCodeRedemption>> GetRedemptionsByCodeAsync(Guid codeId, CancellationToken ct = default);
}
