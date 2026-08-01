using PawTrack.Domain.Subscriptions;

namespace PawTrack.Application.Subscriptions.DTOs;

public sealed record SubscriptionDto(
    Guid Id,
    SubscriptionTier Tier,
    SubscriptionStatus Status,
    string PaymentReference,
    decimal AmountCrc,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ActivatedAt,
    DateTimeOffset? ExpiresAt,
    DateTimeOffset? PaymentReportedAt,
    bool IsActive)
{
    public static SubscriptionDto FromDomain(Subscription s) => new(
        s.Id,
        s.Tier,
        s.Status,
        s.PaymentReference,
        s.AmountCrc,
        s.CreatedAt,
        s.ActivatedAt,
        s.ExpiresAt,
        s.PaymentReportedAt,
        s.IsActive);
}
