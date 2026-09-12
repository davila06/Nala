using PawTrack.Domain.Subscriptions;

namespace PawTrack.Application.Subscriptions.DTOs;

public sealed record SubscriptionDto(
    Guid Id,
    SubscriptionTier Tier,
    SubscriptionStatus Status,
    int BillingMonths,
    string PaymentReference,
    decimal AmountCrc,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ActivatedAt,
    DateTimeOffset? StartsAt,
    DateTimeOffset? ExpiresAt,
    DateTimeOffset? PaymentReportedAt,
    DateTimeOffset? CancellationRequestedAt,
    bool IsActive,
    string? BankReceiptNumber = null,
    Guid? UserId = null,
    Guid? ClinicId = null)
{
    public static SubscriptionDto FromDomain(Subscription s) => new(
        s.Id,
        s.Tier,
        s.Status,
        s.BillingMonths,
        s.PaymentReference,
        s.AmountCrc,
        s.CreatedAt,
        s.ActivatedAt,
        s.StartsAt,
        s.ExpiresAt,
        s.PaymentReportedAt,
        s.CancellationRequestedAt,
        s.IsActive,
        s.BankReceiptNumber,
        s.UserId,
        s.ClinicId);
}
