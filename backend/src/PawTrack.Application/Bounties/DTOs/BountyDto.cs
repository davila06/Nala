using PawTrack.Domain.Bounties;

namespace PawTrack.Application.Bounties.DTOs;

public sealed record BountyDto(
    Guid          Id,
    Guid          LostPetEventId,
    decimal       Amount,
    string        CurrencyCode,
    BountyStatus  Status,
    string        DepositReference,
    decimal       PlatformFee,
    decimal       NetPayoutAmount,
    Guid?         ClaimedByUserId,
    DateTimeOffset  CreatedAt,
    DateTimeOffset? DepositedAt,
    DateTimeOffset? ClaimedAt,
    DateTimeOffset? ReleasedAt)
{
    public static BountyDto FromDomain(Bounty b) => new(
        b.Id, b.LostPetEventId, b.Amount, b.CurrencyCode, b.Status,
        b.DepositReference, b.PlatformFee, b.NetPayoutAmount,
        b.ClaimedByUserId, b.CreatedAt, b.DepositedAt, b.ClaimedAt, b.ReleasedAt);
}
