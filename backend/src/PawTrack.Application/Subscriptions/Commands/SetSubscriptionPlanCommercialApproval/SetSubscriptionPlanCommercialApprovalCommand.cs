using MediatR;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Subscriptions.DTOs;
using PawTrack.Application.Subscriptions.Interfaces;
using PawTrack.Domain.Audit;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Subscriptions.Commands.SetSubscriptionPlanCommercialApproval;

public sealed record SetSubscriptionPlanCommercialApprovalCommand(
    Guid PlanId,
    Guid Version,
    Guid AdminUserId,
    bool IsApproved,
    string? ApprovalReference) : IRequest<Result<SubscriptionPlanDto>>;

public sealed class SetSubscriptionPlanCommercialApprovalCommandHandler(
    ISubscriptionPlanRepository repository,
    IAuditLogRepository auditLogRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<SetSubscriptionPlanCommercialApprovalCommand, Result<SubscriptionPlanDto>>
{
    public async Task<Result<SubscriptionPlanDto>> Handle(
        SetSubscriptionPlanCommercialApprovalCommand request,
        CancellationToken cancellationToken)
    {
        var plan = await repository.GetByIdAsync(request.PlanId, cancellationToken);
        if (plan is null)
            return Result.Failure<SubscriptionPlanDto>("Subscription plan not found.");
        if (plan.Version != request.Version)
            return Result.Failure<SubscriptionPlanDto>("The subscription plan was modified by another administrator.");

        try
        {
            if (request.IsApproved)
            {
                plan.ApproveForCommercialPublication(request.AdminUserId, request.ApprovalReference ?? string.Empty);
            }
            else
            {
                plan.RevokeCommercialPublicationApproval();
            }
        }
        catch (ArgumentException exception)
        {
            return Result.Failure<SubscriptionPlanDto>(exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            return Result.Failure<SubscriptionPlanDto>(exception.Message);
        }

        repository.Update(plan);
        var action = request.IsApproved
            ? AuditAction.SubscriptionPlanCommerciallyApproved
            : AuditAction.SubscriptionPlanCommercialApprovalRevoked;
        await auditLogRepository.AddAsync(AuditLogEntry.Create(
            request.AdminUserId,
            action,
            "SubscriptionPlan",
            plan.Id.ToString(),
            request.IsApproved ? plan.CommercialApprovalReference : "Commercial approval revoked"),
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(SubscriptionPlanDto.FromDomain(plan));
    }
}
