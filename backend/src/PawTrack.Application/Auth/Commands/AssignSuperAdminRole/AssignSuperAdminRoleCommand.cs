using FluentValidation;
using MediatR;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Audit;
using PawTrack.Domain.Auth;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Auth.Commands.AssignSuperAdminRole;

public sealed record AssignSuperAdminRoleCommand(Guid ActorUserId, Guid TargetUserId, string Reason, string MfaCode)
    : IRequest<Result<Unit>>;
public sealed record RevokeSuperAdminRoleCommand(Guid ActorUserId, Guid TargetUserId, string Reason, string MfaCode)
    : IRequest<Result<Unit>>;

public sealed class AssignSuperAdminRoleCommandValidator : AbstractValidator<AssignSuperAdminRoleCommand>
{
    public AssignSuperAdminRoleCommandValidator()
    {
        RuleFor(x => x.ActorUserId).NotEmpty();
        RuleFor(x => x.TargetUserId).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MinimumLength(10).MaximumLength(500);
        RuleFor(x => x.MfaCode).NotEmpty().Length(6);
    }
}

public sealed class RevokeSuperAdminRoleCommandValidator : AbstractValidator<RevokeSuperAdminRoleCommand>
{
    public RevokeSuperAdminRoleCommandValidator()
    {
        RuleFor(x => x.ActorUserId).NotEmpty();
        RuleFor(x => x.TargetUserId).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MinimumLength(10).MaximumLength(500);
        RuleFor(x => x.MfaCode).NotEmpty().Length(6);
    }
}

public sealed class RevokeSuperAdminRoleCommandHandler(
    IUserRepository userRepository,
    IAuditLogRepository auditLogRepository,
    IMfaService mfaService,
    IUnitOfWork unitOfWork)
    : IRequestHandler<RevokeSuperAdminRoleCommand, Result<Unit>>
{
    public async Task<Result<Unit>> Handle(RevokeSuperAdminRoleCommand request, CancellationToken ct)
    {
        var actor = await userRepository.GetByIdAsync(request.ActorUserId, ct);
        if (actor?.Role != UserRole.SuperAdmin || !actor.HasMfa ||
            !mfaService.Verify(actor.MfaSecretProtected!, request.MfaCode))
            return Result.Failure<Unit>("SuperAdmin with fresh MFA verification is required.");
        if (request.ActorUserId == request.TargetUserId)
            return Result.Failure<Unit>("SuperAdmin self-revocation is not allowed.");
        var target = await userRepository.GetByIdAsync(request.TargetUserId, ct);
        if (target?.Role != UserRole.SuperAdmin)
            return Result.Failure<Unit>("Target user is not a SuperAdmin.");
        if (await userRepository.CountByRoleAsync(UserRole.SuperAdmin, ct) <= 1)
            return Result.Failure<Unit>("The last SuperAdmin cannot be revoked.");

        target.RevokeSuperAdminRole();
        userRepository.Update(target);
        await auditLogRepository.AddAsync(AuditLogEntry.Create(
            request.ActorUserId, AuditAction.SuperAdminRevoked, "User", target.Id.ToString(), request.Reason), ct);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success(Unit.Value);
    }
}

public sealed class AssignSuperAdminRoleCommandHandler(
    IUserRepository userRepository,
    IAuditLogRepository auditLogRepository,
    IMfaService mfaService,
    IUnitOfWork unitOfWork)
    : IRequestHandler<AssignSuperAdminRoleCommand, Result<Unit>>
{
    public async Task<Result<Unit>> Handle(AssignSuperAdminRoleCommand request, CancellationToken ct)
    {
        var actor = await userRepository.GetByIdAsync(request.ActorUserId, ct);
        if (actor?.Role != UserRole.SuperAdmin)
            return Result.Failure<Unit>("Only a SuperAdmin can assign the SuperAdmin role.");
        if (!actor.HasMfa || !mfaService.Verify(actor.MfaSecretProtected!, request.MfaCode))
            return Result.Failure<Unit>("Fresh MFA verification is required.");
        if (request.ActorUserId == request.TargetUserId)
            return Result.Failure<Unit>("SuperAdmin self-elevation is not allowed.");

        var target = await userRepository.GetByIdAsync(request.TargetUserId, ct);
        if (target is null) return Result.Failure<Unit>("Target user was not found.");
        if (target.Role == UserRole.SuperAdmin)
            return Result.Failure<Unit>("Target user is already a SuperAdmin.");
        if (!target.IsEmailVerified)
            return Result.Failure<Unit>("Target user must have a verified email.");
        if (!target.HasMfa)
            return Result.Failure<Unit>("Target user must configure MFA before elevation.");

        target.AssignSuperAdminRole();
        userRepository.Update(target);
        await auditLogRepository.AddAsync(AuditLogEntry.Create(
            request.ActorUserId,
            AuditAction.SuperAdminAssigned,
            "User",
            target.Id.ToString(),
            request.Reason), ct);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success(Unit.Value);
    }
}
