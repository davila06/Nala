using MediatR;
using PawTrack.Application.AnimalWelfare.Interfaces;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.AnimalWelfare;
using PawTrack.Domain.Common;

namespace PawTrack.Application.AnimalWelfare.Commands;

public sealed record StartWelfareCaseTriageCommand(Guid CaseId, Guid ActorUserId) : IRequest<Result<bool>>;
public sealed record SetWelfareCaseSeverityCommand(Guid CaseId, Guid ActorUserId, WelfareSeverity Severity) : IRequest<Result<bool>>;
public sealed record AssignWelfareCaseCommand(Guid CaseId, Guid ActorUserId, Guid OrganizationUserId, string Role) : IRequest<Result<bool>>;
public sealed record ReferWelfareCaseCommand(Guid CaseId, Guid ActorUserId, string Destination, string Reason) : IRequest<Result<bool>>;
public sealed record ResolveWelfareCaseCommand(Guid CaseId, Guid ActorUserId, string Resolution) : IRequest<Result<bool>>;
public sealed record DismissWelfareCaseCommand(Guid CaseId, Guid ActorUserId, string Reason) : IRequest<Result<bool>>;
public sealed record CloseWelfareCaseNoActionCommand(Guid CaseId, Guid ActorUserId, string Reason) : IRequest<Result<bool>>;
public sealed record AddWelfareCaseNoteCommand(Guid CaseId, Guid ActorUserId, string Body) : IRequest<Result<bool>>;

internal static class WelfareCaseMutation
{
    public static async Task<Result<bool>> MutateAsync(
        IAnimalWelfareCaseRepository caseRepository,
        IAnimalWelfareAuditRepository auditRepository,
        IUnitOfWork unitOfWork,
        Guid caseId,
        Guid actorUserId,
        Func<AnimalWelfareCase, Result<bool>> mutation,
        WelfareAuditAction action,
        string? details,
        CancellationToken ct)
    {
        var welfareCase = await caseRepository.GetByIdAsync(caseId, ct);
        if (welfareCase is null) return Result.Failure<bool>("Caso de bienestar no encontrado.");

        var result = mutation(welfareCase);
        if (result.IsFailure) return result;

        caseRepository.Update(welfareCase);
        await auditRepository.AddAsync(AnimalWelfareCaseAuditLog.Create(caseId, action, actorUserId, details), ct);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success(true);
    }
}

public sealed class StartWelfareCaseTriageCommandHandler(
    IAnimalWelfareCaseRepository caseRepository,
    IAnimalWelfareAuditRepository auditRepository,
    IUnitOfWork unitOfWork) : IRequestHandler<StartWelfareCaseTriageCommand, Result<bool>>
{
    public Task<Result<bool>> Handle(StartWelfareCaseTriageCommand request, CancellationToken ct) =>
        WelfareCaseMutation.MutateAsync(caseRepository, auditRepository, unitOfWork, request.CaseId, request.ActorUserId,
            c => c.StartTriage(request.ActorUserId), WelfareAuditAction.TriageStarted, null, ct);
}

public sealed class SetWelfareCaseSeverityCommandHandler(
    IAnimalWelfareCaseRepository caseRepository,
    IAnimalWelfareAuditRepository auditRepository,
    IUnitOfWork unitOfWork) : IRequestHandler<SetWelfareCaseSeverityCommand, Result<bool>>
{
    public Task<Result<bool>> Handle(SetWelfareCaseSeverityCommand request, CancellationToken ct) =>
        WelfareCaseMutation.MutateAsync(caseRepository, auditRepository, unitOfWork, request.CaseId, request.ActorUserId,
            c => c.SetSeverity(request.Severity, request.ActorUserId), WelfareAuditAction.SeverityChanged, request.Severity.ToString(), ct);
}

public sealed class AssignWelfareCaseCommandHandler(
    IAnimalWelfareCaseRepository caseRepository,
    IAnimalWelfareAuditRepository auditRepository,
    IUnitOfWork unitOfWork) : IRequestHandler<AssignWelfareCaseCommand, Result<bool>>
{
    public Task<Result<bool>> Handle(AssignWelfareCaseCommand request, CancellationToken ct) =>
        WelfareCaseMutation.MutateAsync(caseRepository, auditRepository, unitOfWork, request.CaseId, request.ActorUserId,
            c => c.AssignTo(request.OrganizationUserId, request.Role, request.ActorUserId), WelfareAuditAction.Assigned, request.Role, ct);
}

public sealed class ReferWelfareCaseCommandHandler(
    IAnimalWelfareCaseRepository caseRepository,
    IAnimalWelfareAuditRepository auditRepository,
    IUnitOfWork unitOfWork) : IRequestHandler<ReferWelfareCaseCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(ReferWelfareCaseCommand request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Destination)) return Result.Failure<bool>("El destino de derivación es requerido.");
        var result = await WelfareCaseMutation.MutateAsync(caseRepository, auditRepository, unitOfWork, request.CaseId, request.ActorUserId,
            c => c.ReferTo(request.ActorUserId, request.Reason), WelfareAuditAction.Referred, request.Destination, ct);
        if (result.IsFailure) return result;

        await auditRepository.AddReferralAsync(AnimalWelfareReferral.Create(request.CaseId, request.Destination, request.ActorUserId, request.Reason), ct);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success(true);
    }
}

public sealed class ResolveWelfareCaseCommandHandler(
    IAnimalWelfareCaseRepository caseRepository,
    IAnimalWelfareAuditRepository auditRepository,
    IUnitOfWork unitOfWork) : IRequestHandler<ResolveWelfareCaseCommand, Result<bool>>
{
    public Task<Result<bool>> Handle(ResolveWelfareCaseCommand request, CancellationToken ct) =>
        WelfareCaseMutation.MutateAsync(caseRepository, auditRepository, unitOfWork, request.CaseId, request.ActorUserId,
            c => c.Resolve(request.ActorUserId, request.Resolution), WelfareAuditAction.Resolved, request.Resolution, ct);
}

public sealed class DismissWelfareCaseCommandHandler(
    IAnimalWelfareCaseRepository caseRepository,
    IAnimalWelfareAuditRepository auditRepository,
    IUnitOfWork unitOfWork) : IRequestHandler<DismissWelfareCaseCommand, Result<bool>>
{
    public Task<Result<bool>> Handle(DismissWelfareCaseCommand request, CancellationToken ct) =>
        WelfareCaseMutation.MutateAsync(caseRepository, auditRepository, unitOfWork, request.CaseId, request.ActorUserId,
            c => c.Dismiss(request.ActorUserId, request.Reason), WelfareAuditAction.Dismissed, request.Reason, ct);
}

public sealed class CloseWelfareCaseNoActionCommandHandler(
    IAnimalWelfareCaseRepository caseRepository,
    IAnimalWelfareAuditRepository auditRepository,
    IUnitOfWork unitOfWork) : IRequestHandler<CloseWelfareCaseNoActionCommand, Result<bool>>
{
    public Task<Result<bool>> Handle(CloseWelfareCaseNoActionCommand request, CancellationToken ct) =>
        WelfareCaseMutation.MutateAsync(caseRepository, auditRepository, unitOfWork, request.CaseId, request.ActorUserId,
            c => c.CloseNoAction(request.ActorUserId, request.Reason), WelfareAuditAction.ClosedNoAction, request.Reason, ct);
}

public sealed class AddWelfareCaseNoteCommandHandler(
    IAnimalWelfareCaseRepository caseRepository,
    IAnimalWelfareAuditRepository auditRepository,
    IUnitOfWork unitOfWork) : IRequestHandler<AddWelfareCaseNoteCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(AddWelfareCaseNoteCommand request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Body)) return Result.Failure<bool>("La nota es requerida.");
        var welfareCase = await caseRepository.GetByIdAsync(request.CaseId, ct);
        if (welfareCase is null) return Result.Failure<bool>("Caso de bienestar no encontrado.");
        await auditRepository.AddNoteAsync(AnimalWelfareCaseNote.Create(request.CaseId, request.ActorUserId, request.Body), ct);
        await auditRepository.AddAsync(AnimalWelfareCaseAuditLog.Create(request.CaseId, WelfareAuditAction.NoteAdded, request.ActorUserId), ct);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success(true);
    }
}
