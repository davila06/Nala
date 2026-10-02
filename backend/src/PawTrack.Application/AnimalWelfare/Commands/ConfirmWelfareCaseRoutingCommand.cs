using MediatR;
using PawTrack.Application.AnimalWelfare.Interfaces;
using PawTrack.Application.AnimalWelfare.Routing;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.AnimalWelfare;
using PawTrack.Domain.Common;

namespace PawTrack.Application.AnimalWelfare.Commands;

public sealed record ConfirmWelfareCaseRoutingCommand(
    Guid CaseId,
    Guid ActorUserId,
    Guid RecipientUserId,
    WelfareReferralRecipientType RecipientType,
    string Reason) : IRequest<Result<bool>>;

public sealed class ConfirmWelfareCaseRoutingCommandHandler(
    IAnimalWelfareCaseRepository caseRepository,
    IAnimalWelfareAuditRepository auditRepository,
    WelfareRoutingService routingService,
    IUnitOfWork unitOfWork)
    : IRequestHandler<ConfirmWelfareCaseRoutingCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(ConfirmWelfareCaseRoutingCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Reason))
            return Result.Failure<bool>("El motivo de confirmación es requerido.");

        var welfareCase = await caseRepository.GetByIdAsync(request.CaseId, cancellationToken);
        if (welfareCase is null)
            return Result.Failure<bool>("Caso de bienestar no encontrado.");
        if (welfareCase.IsClosed)
            return Result.Failure<bool>("No se puede rutear un caso cerrado.");
        if (welfareCase.AssignedOrganizationUserId.HasValue)
            return Result.Failure<bool>("El caso ya tiene un destinatario confirmado.");

        var candidate = (await routingService.GetCandidatesAsync(
                welfareCase.Canton,
                welfareCase.ApproxLat,
                welfareCase.ApproxLng,
                cancellationToken))
            .FirstOrDefault(item => item.UserId == request.RecipientUserId && item.RecipientType == request.RecipientType);
        if (candidate is null)
            return Result.Failure<bool>("El destinatario no está verificado, activo o dentro de la cobertura del caso.");

        var assigned = welfareCase.AssignTo(candidate.UserId, candidate.RecipientType.ToString(), request.ActorUserId);
        if (assigned.IsFailure) return assigned;

        caseRepository.Update(welfareCase);
        await auditRepository.AddReferralAsync(AnimalWelfareReferral.CreateConfirmed(
            welfareCase.Id,
            candidate.UserId,
            candidate.RecipientType,
            candidate.OrganizationName,
            request.ActorUserId,
            request.Reason), cancellationToken);
        await auditRepository.AddAsync(AnimalWelfareCaseAuditLog.Create(
            welfareCase.Id,
            WelfareAuditAction.Assigned,
            request.ActorUserId,
            $"Confirmed routing to {candidate.RecipientType}:{candidate.UserId}; distanceMetres={candidate.DistanceMetres?.ToString() ?? "canton"}; reason={request.Reason.Trim()}"), cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(true);
    }
}
