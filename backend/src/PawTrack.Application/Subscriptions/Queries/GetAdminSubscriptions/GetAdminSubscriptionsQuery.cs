using MediatR;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Subscriptions.DTOs;
using PawTrack.Application.Subscriptions.Interfaces;
using PawTrack.Domain.Common;
using PawTrack.Domain.Subscriptions;

namespace PawTrack.Application.Subscriptions.Queries.GetAdminSubscriptions;

public sealed record AdminSubscriptionDto(
    Guid Id,
    Guid? UserId,
    Guid? ClinicId,
    SubscriptionTier Tier,
    SubscriptionStatus Status,
    string PaymentReference,
    decimal AmountCrc,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ActivatedAt,
    DateTimeOffset? ExpiresAt,
    DateTimeOffset? PaymentReportedAt)
{
    public static AdminSubscriptionDto FromDomain(Subscription s) => new(
        s.Id, s.UserId, s.ClinicId, s.Tier, s.Status,
        s.PaymentReference, s.AmountCrc, s.CreatedAt, s.ActivatedAt, s.ExpiresAt, s.PaymentReportedAt);
}

public sealed record GetAdminSubscriptionsQuery(bool PendingOnly = false, int Skip = 0, int Take = 50)
    : IRequest<Result<IReadOnlyList<AdminSubscriptionDto>>>;

public sealed class GetAdminSubscriptionsQueryHandler(ISubscriptionRepository subscriptionRepository)
    : IRequestHandler<GetAdminSubscriptionsQuery, Result<IReadOnlyList<AdminSubscriptionDto>>>
{
    public async Task<Result<IReadOnlyList<AdminSubscriptionDto>>> Handle(
        GetAdminSubscriptionsQuery request, CancellationToken cancellationToken)
    {
        var subs = request.PendingOnly
            ? await subscriptionRepository.GetPendingAsync(cancellationToken)
            : await subscriptionRepository.GetAllPagedAsync(request.Skip, request.Take, cancellationToken);

        return Result.Success<IReadOnlyList<AdminSubscriptionDto>>(
            subs.Select(AdminSubscriptionDto.FromDomain).ToList());
    }
}
