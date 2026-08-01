using MediatR;
using PawTrack.Application.Subscriptions.DTOs;
using PawTrack.Application.Subscriptions.Interfaces;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Subscriptions.Commands.ReportPayment;

/// <summary>
/// Subscriber self-reports that they have sent the SINPE payment.
/// Sets PaymentReportedAt so admin can see and quickly activate.
/// </summary>
public sealed record ReportPaymentCommand(Guid SubscriptionId, Guid RequestingUserId)
    : IRequest<Result<SubscriptionDto>>;

public sealed class ReportPaymentCommandHandler(
    ISubscriptionRepository subscriptionRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<ReportPaymentCommand, Result<SubscriptionDto>>
{
    public async Task<Result<SubscriptionDto>> Handle(
        ReportPaymentCommand request, CancellationToken cancellationToken)
    {
        var sub = await subscriptionRepository.GetByIdAsync(request.SubscriptionId, cancellationToken);
        if (sub is null)
            return Result.Failure<SubscriptionDto>("Subscription not found.");

        if (sub.UserId != request.RequestingUserId && sub.ClinicOwnerId != request.RequestingUserId)
            return Result.Failure<SubscriptionDto>("Access denied.");

        sub.ReportPaymentSent();
        subscriptionRepository.Update(sub);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(SubscriptionDto.FromDomain(sub));
    }
}
