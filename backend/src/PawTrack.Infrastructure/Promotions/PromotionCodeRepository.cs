using Microsoft.EntityFrameworkCore;
using PawTrack.Application.Promotions.Interfaces;
using PawTrack.Domain.Promotions;
using PawTrack.Infrastructure.Persistence;

namespace PawTrack.Infrastructure.Promotions;

public sealed class PromotionCodeRepository(PawTrackDbContext db) : IPromotionCodeRepository
{
    public async Task AddRangeAsync(IEnumerable<PromotionCode> codes, CancellationToken ct = default) =>
        await db.PromotionCodes.AddRangeAsync(codes, ct);

    public Task<PromotionCode?> GetByCodeAsync(string code, CancellationToken ct = default) =>
        db.PromotionCodes.AsTracking()
            .FirstOrDefaultAsync(c => c.Code == code, ct);

    public async Task<IReadOnlyList<PromotionCode>> GetAllAsync(CancellationToken ct = default) =>
        await db.PromotionCodes.AsNoTracking()
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(ct);

    public void Update(PromotionCode code) => db.PromotionCodes.Update(code);

    public Task<bool> HasUserRedeemedAsync(Guid userId, Guid promotionCodeId, CancellationToken ct = default) =>
        db.PromotionCodeRedemptions.AnyAsync(
            r => r.UserId == userId && r.PromotionCodeId == promotionCodeId, ct);

    public Task<bool> HasUserActivePromoSubscriptionAsync(Guid userId, CancellationToken ct = default) =>
        db.Subscriptions.AnyAsync(
            s => s.UserId == userId &&
                 s.RedeemedPromotionCodeId != null &&
                 s.Status == PawTrack.Domain.Subscriptions.SubscriptionStatus.Active &&
                 s.ExpiresAt > DateTimeOffset.UtcNow, ct);

    public async Task AddRedemptionAsync(PromotionCodeRedemption redemption, CancellationToken ct = default) =>
        await db.PromotionCodeRedemptions.AddAsync(redemption, ct);

    public async Task<IReadOnlyList<PromotionCodeRedemption>> GetRedemptionsByCodeAsync(
        Guid codeId, CancellationToken ct = default) =>
        await db.PromotionCodeRedemptions.AsNoTracking()
            .Where(r => r.PromotionCodeId == codeId)
            .OrderByDescending(r => r.RedeemedAt)
            .ToListAsync(ct);
}
